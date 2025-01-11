//----------------------------------------------
// DynamoDB Helper
// Copyright © 2014-2015 OuijaPaw Games LLC
//----------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Amazon.DynamoDBv2.Model;
using UnityEngine;

namespace DDBHelper
{
	/// <summary>
	/// The idea is that here is access to low-level API calls for DynamoDB in some kind of actual
	/// functional, readable, understandable manner.
	/// 
	/// You create a DBObject.
	/// DBObject handles the data.
	/// DBWorker handles the CRUD operations via it's own thread, using callbacks for validation.
	/// attributeValues are all of the item's data
	/// DBObject will only update specific items which are added to attributeUpdates
	/// Once updated, attributeValues is cleared, as they are now updated
	/// 
	/// You can read/write memory streams or byte[] arrays with this as well, which are compressed
	/// to reduce network traffic / database object size
	/// 
	/// Be aware the if the total size of the object is over 64kb, it will be additional read/writes
	/// </summary>
	public class DBObject
	{
		public const string HASH_KEY = "SortKey";
		public const string RANGE_KEY = "Type";
		public AttributeValue mAttribHashKey { get; private set; }
		public AttributeValue mAttribRangeKey { get; private set; }

		public bool mIsDirty { get; set; }
		public Dictionary<string, AttributeValue> mItemKeys { get; private set; }
		public Dictionary<string, AttributeValue> mAttributeValues { get; set; }
		public Dictionary<string, AttributeValueUpdate> mAttributeUpdates { get; set; }
		public List<string> mAttributesToGet;

		private NextMethod msgNextMethod = null;
		private GameObject msgGameObject = null;
		private MonoBehaviour msgMonoBehavor = null;
		private string msgMethodName = null;
		public object msgData { get; set; }

		private TypeConverter mTypeConverter;
		private List<MemoryStream> mTempMemStreamList = new List<MemoryStream>();
		private List<String> mTempStringList = new List<String>();

		/// <summary>
		/// Empty constructor sets values we need, but it is kept private
		/// as we do not want others to use this constructor
		/// </summary>
		private DBObject()
		{
			msgNextMethod = null;
			msgData = null;
			mItemKeys = new Dictionary<string, AttributeValue>();
			mAttributeValues = new Dictionary<string, AttributeValue>();
			mAttributeUpdates = new Dictionary<string, AttributeValueUpdate>();
		}

		/// <summary>
		/// Public constructor to ensure the hash/range key are set
		/// </summary>
		/// <param name="hashItemOnly"></param>
		/// <param name="ignoreCase"></param>
		public DBObject(string hashItemOnly, bool ignoreCase = false)
			: this()
		{
			// remove this is you want to check case
			if (!ignoreCase)
				hashItemOnly = hashItemOnly.ToLower();

			// these are set only once and used repeatedly
			mAttribHashKey = new AttributeValue() { S = hashItemOnly };
			mAttributeValues.Add(HASH_KEY, mAttribHashKey);

			mItemKeys.Add(HASH_KEY, mAttribHashKey);

			mIsDirty = true;
		}

		/// <summary>
		/// Public constructor to ensure the hash/range key are set
		/// </summary>
		/// <param name="hashItem"></param>
		/// <param name="rangeItem"></param>
		/// <param name="ignoreCase"></param>
		public DBObject(string hashItem, string rangeItem, bool ignoreCase = false)
			: this()
		{
			// remove this is you want to check case
			if (!ignoreCase)
			{
				hashItem = hashItem.ToLower();
				rangeItem = rangeItem.ToLower();
			}

			// these are set only once and used repeatedly
			mAttribHashKey = new AttributeValue() { S = hashItem };
			mAttribRangeKey = new AttributeValue() { S = rangeItem };

			mItemKeys.Add(HASH_KEY, mAttribHashKey);
			mItemKeys.Add(RANGE_KEY, mAttribRangeKey);

			mIsDirty = true;
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~DBObject()
		{
			msgData = null;
			msgNextMethod = null;
			msgGameObject = null;
			msgMonoBehavor = null;
		}

		#region Delegate Responses
		/// <summary>
		/// Callback when dbWorker.CreateItem completes
		/// </summary>
		/// <param name="asyncResult"></param>
		/// <param name="e"></param>
		public void DBObject_OnCreated(bool success, Exception e = null)
		{
			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("DBObject Item Created, success=" + success);
			if (e != null)
				DBTools.PrintException("DBObject DBObject_OnCreated", e);
			InvokeNextMessage();
		}

		/// <summary>
		/// Callback when dbWorker.UpdateItem completes
		/// </summary>
		/// <param name="asyncResult"></param>
		/// <param name="e"></param>
		public void DBObject_OnUpdated(bool success, Exception e = null)
		{
			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("DBObject Item Updated, success=" + success);
			if (e != null)
				DBTools.PrintException("DBObject DBObject_OnUpdated", e);
			InvokeNextMessage();
		}

		/// <summary>
		/// Callback when dbWorker.DeleteItem completes
		/// </summary>
		/// <param name="asyncResult"></param>
		/// <param name="e"></param>
		public void DBObject_OnDeleted(bool success, Exception e = null)
		{
			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("DBObject Item Deleted, success=" + success);
			if (e != null)
				DBTools.PrintException("DBObject DBObject_OnDeleted", e);
			InvokeNextMessage();
		}

		/// <summary>
		/// Callback when dbWorker.LoadItem completes
		/// </summary>
		/// <param name="success"></param>
		/// <param name="e"></param>
		public void DBObject_OnLoaded(bool success, Exception e = null)
		{
			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("DBObject Item Loaded, success=" + success);
			if (e != null)
				DBTools.PrintException("DBObject DBObject_OnLoaded", e);
			InvokeNextMessage();
		}

		/// <summary>
		/// Callback when dbWorker.LoadItem completes
		/// </summary>
		/// <param name="success"></param>
		/// <param name="e"></param>
		public void DBObject_OnExists(bool success, Exception e = null)
		{
			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("DBObject Item hash=" + mAttribHashKey.S.ToString() + " range=" + mAttribRangeKey.S.ToString() + " exists=" + success);
			if (e != null)
				DBTools.PrintException("DBObject DBObject_OnLoaded", e);
			InvokeNextMessage();
		}
		#endregion

		/// <summary>
		/// Easy access lookup to what the DBObject hash-key currently is
		/// </summary>
		/// <returns></returns>
		public string GetHashKey()
		{
			AttributeValue attrib;
			string val = "NULL";

			mItemKeys.TryGetValue(HASH_KEY, out attrib);

			if (attrib != null)
			{
				if (!string.IsNullOrEmpty(attrib.S))
					val = attrib.S;
				else if (!string.IsNullOrEmpty(attrib.N))
					val = attrib.N;
			}

			return val;
		}

		/// <summary>
		/// Easy access lookup to what the DBObject range-key currently is
		/// </summary>
		/// <returns></returns>
		public string GetRangeKey()
		{
			AttributeValue attrib;
			string val = "NULL";

			mItemKeys.TryGetValue(RANGE_KEY, out attrib);

			if (attrib != null)
			{
				if (!string.IsNullOrEmpty(attrib.S))
					val = attrib.S;
				else if (!string.IsNullOrEmpty(attrib.N))
					val = attrib.N;
			}

			return val;

		}

		/// <summary>
		/// Method to prepare a message to be sent upon a response
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="nextMethod"></param>
		public void PrepareNextMessage(GameObject obj, string nextMethod)
		{
			msgGameObject = obj;
			msgMethodName = nextMethod;
		}

		/// <summary>
		/// Method to prepare a message to be sent upon a response
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="nextMethod"></param>
		public void PrepareNextMessage(MonoBehaviour obj, string nextMethod)
		{
			msgMonoBehavor = obj;
			msgMethodName = nextMethod;
		}

		/// <summary>
		/// Method to prepare a message to be sent upon a response
		/// </summary>
		/// <param name="nextMethod"></param>
		public void PrepareNextMessage(NextMethod nextMethod)
		{
			msgNextMethod = nextMethod;
		}

		/// <summary>
		/// Generic invoke for sending a message upon a response
		/// </summary>
		private void InvokeNextMessage()
		{
			if (msgNextMethod != null)
			{
				msgNextMethod();
				msgNextMethod = null;
			}
			else if (!string.IsNullOrEmpty(msgMethodName))
			{
				if (msgGameObject != null)
				{
					DBUnityHelper.Register(new SendMessageContext(msgGameObject, msgMethodName, msgData, SendMessageOptions.RequireReceiver));
					msgGameObject = null;
				}
				else if (msgMonoBehavor != null)
				{
					DBUnityHelper.Register(new SendCoroutineContext(msgMonoBehavor, msgMethodName, msgData));
					msgMonoBehavor = null;
				}
				msgMethodName = null;
				msgData = null;
			}
		}

		/// <summary>
		/// If you're not using a persisent DBObject, this is an easy way to make sure that any
		/// updates you want to do to the DDB Item will be the only thing you're changing/updating
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void PrepareUpdateString(string key, string value, bool clearDictionary = false)
		{
			if (clearDictionary)
				mAttributeUpdates.Clear();
			mAttributeUpdates.Add(key, new AttributeValueUpdate() { Value = new AttributeValue() { S = value } });
		}

		/// <summary>
		/// If you're not using a persisent DBObject, this is an easy way to make sure that any
		/// updates you want to do to the DDB Item will be the only thing you're changing/updating
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void PrepareUpdateNumber<T>(string key, T value, bool clearDictionary = false)
		{
			if (clearDictionary)
				mAttributeUpdates.Clear();
			mAttributeUpdates.Add(key, new AttributeValueUpdate() { Value = new AttributeValue() { N = value.ToString() } });
		}

		/// <summary>
		/// If you're not using a persisent DBObject, this is an easy way to make sure that any
		/// updates you want to do to the DDB Item will be the only thing you're changing/updating
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void PrepareUpdateBool(string key, bool value, bool clearDictionary = false)
		{
			if (clearDictionary)
				mAttributeUpdates.Clear();
			mAttributeUpdates.Add(key, new AttributeValueUpdate() { Value = new AttributeValue() { BOOL = value } });
		}

		/// <summary>
		/// If you're not using a persisent DBObject, this is an easy way to make sure that any
		/// updates you want to do to the DDB Item will be the only thing you're changing/updating
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void PrepareUpdateListInt(string key, List<int> value, bool clearDictionary = false)
		{
			if (clearDictionary)
				mAttributeUpdates.Clear();

			List<AttributeValue> iList = new List<AttributeValue>();

			foreach (int i in value)
				iList.Add(new AttributeValue() { N = i.ToString() });

			mAttributeUpdates.Add(key, new AttributeValueUpdate() { Value = new AttributeValue() { L = iList } });
		}

		/// <summary>
		/// If you're not using a persisent DBObject, this is an easy way to make sure that any
		/// updates you want to do to the DDB Item will be the only thing you're changing/updating
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void PrepareUpdateListInt(string key, List<List<int>> value, bool clearDictionary = false)
		{
			if (clearDictionary)
				mAttributeUpdates.Clear();

			List<AttributeValue> oList = new List<AttributeValue>();
			List<AttributeValue> iList;

			foreach (List<int> intList in value)
			{
				iList = new List<AttributeValue>();
				foreach (int i in intList)
					iList.Add(new AttributeValue() { N = i.ToString() });
				oList.Add(new AttributeValue() { L = iList });
			}

			mAttributeUpdates.Add(key, new AttributeValueUpdate() { Value = new AttributeValue() { L = oList } });
		}

		/// <summary>
		/// If you're not using a persisent DBObject, this is an easy way to make sure that any
		/// updates you want to do to the DDB Item will be the only thing you're changing/updating
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void PrepareUpdateListInt(string key, List<List<List<int>>> value, bool clearDictionary = false)
		{
			if (clearDictionary)
				mAttributeUpdates.Clear();

			List<AttributeValue> n0List = new List<AttributeValue>();
			List<AttributeValue> n1List;
			List<AttributeValue> n2List;

			foreach (var outerList in value)
			{
				n1List = new List<AttributeValue>();
				foreach (List<int> innerList in outerList)
				{
					n2List = new List<AttributeValue>();
					foreach (int i in innerList)
						n2List.Add(new AttributeValue() { N = i.ToString() });
					n1List.Add(new AttributeValue() { L = n2List });
				}
				n0List.Add(new AttributeValue() { L = n1List });
			}

			mAttributeUpdates.Add(key, new AttributeValueUpdate() { Value = new AttributeValue() { L = n0List } });
		}


		/// <summary>
		/// After loading an object, this is how you get the data
		/// if a default value is provided, it will be the value if it cannot be found
		/// NOTE -- key is case sensitive!  So if values aren't returning, make sure case is correct!
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public string GetString(string key, string defaultValue = null)
		{
			string str = defaultValue ?? "";
			AttributeValue attrib;
			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - GetString AttributeValue for Key " + key + " val=" + attrib.S.ToString());
				str = attrib.S;
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - GetString AttributeValue for Key " + key + " could NOT be found");
			}
			return str;
		}

		/// <summary>
		/// After loading an object, this is how you get the data
		/// Returns 0 if no data can be found
		/// NOTE -- key is case sensitive!  So if values aren't returning, make sure case is correct!
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
		public T GetNumber<T>(string key)
		{
			T val;
			AttributeValue attrib;
			mTypeConverter = TypeDescriptor.GetConverter(typeof(T));
			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - GetNumber AttributeValue Found for Key " + key + " val=" + attrib.N.ToString());
				val = (T)mTypeConverter.ConvertFromString(attrib.N);
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - GetNumber AttributeValue for Key " + key + " could NOT be found");
				val = default(T);
			}
			return val;
		}

		/// <summary>
		/// After loading an object, this is how you get the data
		/// if a default value is provided, it will be the value if it cannot be found
		/// NOTE -- key is case sensitive!  So if values aren't returning, make sure case is correct!
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public T GetNumber<T>(string key, T defaultValue)
		{
			T val = defaultValue;
			AttributeValue attrib;
			mTypeConverter = TypeDescriptor.GetConverter(typeof(T));
			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - GetNumber AttributeValue Found for Key " + key + " val=" + attrib.N.ToString());
				val = (T)mTypeConverter.ConvertFromString(attrib.N);
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - GetNumber AttributeValue for Key " + key + " could NOT be found");
			}
			return val;
		}

		/// <summary>
		/// After loading an object, this is how you get the data
		/// if a default value is provided, it will be the value if it cannot be found
		/// NOTE -- key is case sensitive!  So if values aren't returning, make sure case is correct!
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public bool GetBool(string key, bool defaultValue = false)
		{
			bool val = defaultValue;
			AttributeValue attrib;
			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				val = attrib.BOOL;
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - GetBool AttributeValue Found for Key " + key + " val=" + val.ToString());
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - GetBool AttributeValue for Key " + key + " could NOT be found");
			}
			return val;
		}

		/// <summary>
		/// After loading an object, this is how you get the data
		/// if a default value is provided, it will be the value if it cannot be found
		/// NOTE -- key is case sensitive!  So if values aren't returning, make sure case is correct!
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public T GetBinary<T>(string key, T defaultValue)
		{
			T val = defaultValue;
			AttributeValue attrib;
			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				val = FromGzipMemoryStream<T>(attrib.B);
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - GetBinary AttributeValue Found for Key " + key + " val=" + val.ToString());
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - GetBinary AttributeValue for Key " + key + " could NOT be found");
				val = default(T);
			}
			return val;
		}

		/// <summary>
		/// After loading an object, this is how you get the data
		/// Returns 0 if nothing found
		/// NOTE -- key is case sensitive!  So if values aren't returning, make sure case is correct!
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
		public T GetBinary<T>(string key)
		{
			T val;
			AttributeValue attrib;
			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				val = FromGzipMemoryStream<T>(attrib.B);
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - GetBinary AttributeValue Found for Key " + key + " val=" + val.ToString());
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - GetBinary AttributeValue for Key " + key + " could NOT be found");
				mTypeConverter = TypeDescriptor.GetConverter(typeof(T));
				val = (T)mTypeConverter.ConvertFromString("0");
			}
			return val;
		}

		/// <summary>
		/// After loading an object, this is how you get the data
		/// if a default value is provided, it will be the value if it cannot be found
		/// NOTE -- key is case sensitive!  So if values aren't returning, make sure case is correct!
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public List<string> GetStringSet(string key, List<string> list = null)
		{
			try
			{
				AttributeValue attrib;
				if (mAttributeValues.TryGetValue(key, out attrib))
				{
					list = attrib.SS;
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - GetStringSet AttributeValue Found Count=" + list.Count);
				}
				else
				{
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - GetStringSet AttributeValue for Key " + key + " could NOT be found");
				}
			}
			catch (System.Exception e)
			{
				Debug.Log("GetStringSet - Exception = " + e.Message);
			}
			return list;
		}

		/// <summary>
		/// After loading an object, this is how you get the data
		/// if a default value is provided, it will be the value if it cannot be found
		/// NOTE -- key is case sensitive!  So if values aren't returning, make sure case is correct!
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public List<T> GetNumberSet<T>(string key, List<T> list = null)
		{
			try
			{
				AttributeValue attrib;
				if (mAttributeValues.TryGetValue(key, out attrib))
				{
					mTempStringList.Clear();
					mTempStringList = attrib.NS;
					mTypeConverter = TypeDescriptor.GetConverter(typeof(T));
					list = mTempStringList.ConvertAll(x => (T)mTypeConverter.ConvertFromString(x));
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - GetNumberSet AttributeValue Found Count=" + list.Count);
				}
				else
				{
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - GetNumberSet AttributeValue for Key " + key + " could NOT be found");
				}
			}
			catch (System.Exception e)
			{
				Debug.Log("GetNumberSet - Exception = " + e.Message);
			}
			return list;
		}

		/// <summary>
		/// After loading an object, this is how you get the data
		/// if a default value is provided, it will be the value if it cannot be found
		/// NOTE -- key is case sensitive!  So if values aren't returning, make sure case is correct!
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public List<T> GetBinarySet<T>(string key, List<T> list = null)
		{
			try
			{
				AttributeValue attrib;
				if (mAttributeValues.TryGetValue(key, out attrib))
				{
					mTempMemStreamList.Clear();
					mTempMemStreamList = attrib.BS;
					mTypeConverter = TypeDescriptor.GetConverter(typeof(T));
					list = mTempMemStreamList.ConvertAll(x => FromGzipMemoryStream<T>(x));
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - GetBinarySet AttributeValue Found Count=" + list.Count);
				}
				else
				{
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - GetBinarySet AttributeValue for Key " + key + " could NOT be found");
				}
			}
			catch (System.Exception e)
			{
				Debug.Log("GetNumberSet - Exception = " + e.Message);
			}
			return list;
		}

		/// <summary>
		/// Updates the local DBObject with the new key-value pair
		/// Checks if the key-value pair exists already
		/// - If data is the same, it will not update
		/// - If data is different, will update and mark dirty
		/// - If data is not found, will create/add key-value pair and mark dirty
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void SetString(string key, string value)
		{
			AttributeValue attrib;
			AttributeValueUpdate update;

			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				if (attrib.S != value)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - Update value pair for key:" + key + " val:" + value);
					update = new AttributeValueUpdate();
					update.Value = new AttributeValue();
					update.Value.S = value;
					mAttributeUpdates.Add(key, update);
					// update current values so a new read is not necessary
					attrib.S = value;
					mIsDirty = true;
				}
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - New value pair created for key:" + key + " val:" + value);
				update = new AttributeValueUpdate();
				update.Value = new AttributeValue();
				update.Value.S = value;
				mAttributeUpdates.Add(key, update);
				// Create current values so a new read is not necessary
				attrib = new AttributeValue();
				attrib.S = value;
				mAttributeValues.Add(key, attrib);
				mIsDirty = true;
			}
		}

		/// <summary>
		/// Updates the local DBObject with the new key-value pair
		/// Checks if the key-value pair exists already
		/// - If data is the same, it will not update
		/// - If data is different, will update and mark dirty
		/// - If data is not found, will create/add key-value pair and mark dirty
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void SetBool(string key, bool value)
		{
			AttributeValue attrib;
			AttributeValueUpdate update;

			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				if (attrib.BOOL != value)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - Update value pair for key:" + key + " val:" + value);
					update = new AttributeValueUpdate();
					update.Value = new AttributeValue();
					update.Value.BOOL = value;
					mAttributeUpdates.Add(key, update);
					// update current values so a new read is not necessary
					attrib.BOOL = value;
					mIsDirty = true;
				}
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - New value pair created for key:" + key + " val:" + value);
				update = new AttributeValueUpdate();
				update.Value = new AttributeValue();
				update.Value.BOOL = value;
				mAttributeUpdates.Add(key, update);
				// Create current values so a new read is not necessary
				attrib = new AttributeValue();
				attrib.BOOL = value;
				mAttributeValues.Add(key, attrib);
				mIsDirty = true;
			}
		}
		
		/// <summary>
		/// Updates the local DBObject with the new key-value pair
		/// Checks if the key-value pair exists already
		/// - If data is the same, it will not update
		/// - If data is different, will update and mark dirty
		/// - If data is not found, will create/add key-value pair and mark dirty
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void SetNumber<T>(string key, T value)
		{
			AttributeValue attrib;
			AttributeValueUpdate update;
			mTypeConverter = TypeDescriptor.GetConverter(typeof(T));
			string numToString = mTypeConverter.ConvertToString(value);

			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				if (attrib.N != numToString)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - Update value pair for key:" + key + " val:" + value);
					update = new AttributeValueUpdate();
					update.Value = new AttributeValue();
					update.Value.N = numToString;
					mAttributeUpdates.Add(key, update);
					// update current values so a new read is not necessary
					attrib.N = numToString;
					mIsDirty = true;
				}
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - New value pair created for key:" + key + " val:" + value);
				update = new AttributeValueUpdate();
				update.Value = new AttributeValue();
				update.Value.N = numToString;
				mAttributeUpdates.Add(key, update);
				// Create current values so a new read is not necessary
				attrib = new AttributeValue();
				attrib.N = numToString;
				mAttributeValues.Add(key, attrib);
				mIsDirty = true;
			}
		}

		/// <summary>
		/// Updates the local DBObject with the new key-value pair
		/// Checks if the key-value pair exists already
		/// - If data is the same, it will not update
		/// - If data is different, will update and mark dirty
		/// - If data is not found, will create/add key-value pair and mark dirty
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void SetBinary<T>(string key, T value)
		{
			AttributeValue attrib;
			AttributeValueUpdate update;
			MemoryStream stream = ToGzipMemoryStream<T>(value);

			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				if (attrib.B != stream)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - Update value pair for key:" + key + " val:" + value);
					update = new AttributeValueUpdate();
					update.Value = new AttributeValue();
					update.Value.B = stream;
					mAttributeUpdates.Add(key, update);
					// update current values so a new read is not necessary
					attrib.B = stream;
					mIsDirty = true;
				}
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - New value pair created for key:" + key + " val:" + value);
				update = new AttributeValueUpdate();
				update.Value = new AttributeValue();
				update.Value.B = stream;
				mAttributeUpdates.Add(key, update);
				// Create current values so a new read is not necessary
				attrib = new AttributeValue();
				attrib.B = stream;
				mAttributeValues.Add(key, attrib);
				mIsDirty = true;
			}
		}

		/// <summary>
		/// Updates the local DBObject with the new key-value pair
		/// Checks if the key-value pair exists already
		/// - If data is the same, it will not update
		/// - If data is different, will update and mark dirty
		/// - If data is not found, will create/add key-value pair and mark dirty
		/// </summary>
		/// <param name="key"></param>
		/// <param name="list"></param>
		public void SetStringSet(string key, List<string> list)
		{
			AttributeValue attrib;
			AttributeValueUpdate update;

			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				if (attrib.SS != list)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - Update value pair for key:" + key + " listCount:" + list.Count);
					update = new AttributeValueUpdate();
					update.Value = new AttributeValue();
					update.Value.SS = list;
					mAttributeUpdates.Add(key, update);
					// update current values so a new read is not necessary
					attrib.SS = list;
					mIsDirty = true;
				}
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - New value pair created for key:" + key + " listCount:" + list.Count);
				update = new AttributeValueUpdate();
				update.Value = new AttributeValue();
				update.Value.SS = list;
				mAttributeUpdates.Add(key, update);
				// Create current values so a new read is not necessary
				attrib = new AttributeValue();
				attrib.SS = list;
				mAttributeValues.Add(key, attrib);
				mIsDirty = true;
			}
		}

		/// <summary>
		/// Updates the local DBObject with the new key-value pair
		/// Checks if the key-value pair exists already
		/// - If data is the same, it will not update
		/// - If data is different, will update and mark dirty
		/// - If data is not found, will create/add key-value pair and mark dirty
		/// </summary>
		/// <param name="key"></param>
		/// <param name="list"></param>
		public void SetNumberSet<T>(string key, List<T> list)
		{
			AttributeValue attrib;
			AttributeValueUpdate update;
			mTypeConverter = TypeDescriptor.GetConverter(typeof(T));

			mTempStringList.Clear();
			for (int i = 0; i < list.Count; i++)
				mTempStringList.Add(mTypeConverter.ConvertToString(list[i]));

			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				if (attrib.NS != mTempStringList)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - Update value pair for key:" + key + " listCount:" + list.Count);
					update = new AttributeValueUpdate();
					update.Value = new AttributeValue();
					update.Value.NS = mTempStringList;
					mAttributeUpdates.Add(key, update);
					// update current values so a new read is not necessary
					attrib.NS = mTempStringList;
					mIsDirty = true;
				}
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - New value pair created for key:" + key + " listCount:" + list.Count);
				update = new AttributeValueUpdate();
				update.Value = new AttributeValue();
				update.Value.NS = mTempStringList;
				mAttributeUpdates.Add(key, update);
				// Create current values so a new read is not necessary
				attrib = new AttributeValue();
				attrib.NS = mTempStringList;
				mAttributeValues.Add(key, attrib);
				mIsDirty = true;
			}
		}

		/// <summary>
		/// Updates the local DBObject with the new key-value pair
		/// Checks if the key-value pair exists already
		/// - If data is the same, it will not update
		/// - If data is different, will update and mark dirty
		/// - If data is not found, will create/add key-value pair and mark dirty
		/// </summary>
		/// <param name="key"></param>
		/// <param name="list"></param>
		public void SetBinarySet<T>(string key, List<T> list)
		{
			AttributeValue attrib;
			AttributeValueUpdate update;

			mTempMemStreamList.Clear();
			for (int i = 0; i < list.Count; i++)
				mTempMemStreamList.Add(ToGzipMemoryStream<T>(list[i]));

			if (mAttributeValues.TryGetValue(key, out attrib))
			{
				if (attrib.BS != mTempMemStreamList)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBObject - Update value pair for key:" + key + " listCount:" + list.Count);
					update = new AttributeValueUpdate();
					update.Value = new AttributeValue();
					update.Value.BS = mTempMemStreamList;
					mAttributeUpdates.Add(key, update);
					// update current values so a new read is not necessary
					attrib.BS = mTempMemStreamList;
					mIsDirty = true;
				}
			}
			else
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBObject - New value pair created for key:" + key + " listCount:" + list.Count);
				update = new AttributeValueUpdate();
				update.Value = new AttributeValue();
				update.Value.BS = mTempMemStreamList;
				mAttributeUpdates.Add(key, update);
				// Create current values so a new read is not necessary
				attrib = new AttributeValue();
				attrib.BS = mTempMemStreamList;
				mAttributeValues.Add(key, attrib);
				mIsDirty = true;
			}
		}

		/// <summary>
		/// Clears out all dictionaries
		/// </summary>
		public void ClearDictionaries()
		{
			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("DBObject - Clearing all dictionaries for object");
			mItemKeys.Clear();
			mAttributeValues.Clear();
			mAttributeUpdates.Clear();
		}

		/// <summary>
		/// Used for serializing types into binary streams
		/// Compresses the stream so less data is being sent over network
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		public static MemoryStream ToGzipMemoryStream<T>(T value)
		{
			MemoryStream output = new MemoryStream();
			try
			{
				Ionic.Zlib.GZipStream zStream;
				BinaryFormatter formatter = new BinaryFormatter();
				using (zStream = new Ionic.Zlib.GZipStream(output, Ionic.Zlib.CompressionMode.Compress, true))
				{
					formatter.Serialize(zStream, value);
				}
			}
			catch (System.Exception e)
			{
				Debug.Log("ToGzipMemoryStream msg=" + e.Message);
			}
			return output;
		}

		/// <summary>
		/// Used for deserializing types from binary streams
		/// Decompresses the stream that was saved with ToGzipMemoryStream
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="stream"></param>
		/// <returns></returns>
		public static T FromGzipMemoryStream<T>(MemoryStream stream)
		{
			T temp = default(T);
			try
			{
				Ionic.Zlib.GZipStream zStream;
				BinaryFormatter formatter = new BinaryFormatter();
				using (zStream = new Ionic.Zlib.GZipStream(stream, Ionic.Zlib.CompressionMode.Decompress))
				{
					temp = (T)formatter.Deserialize(zStream);
				}
			}
			catch (System.Exception e)
			{
				Debug.Log("FromGzipMemoryStream msg=" + e.Message);
			}
			return temp;

		}
	}
}
