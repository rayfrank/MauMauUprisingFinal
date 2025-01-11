//----------------------------------------------
// DynamoDB Helper
// Copyright © 2014-2015 OuijaPaw Games LLC
//----------------------------------------------

using UnityEngine;
using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace DDBHelper
{
	public enum AttribType : byte
	{
		S,
		N,
		B
	}

	public enum CapacityType : int
	{
		NONE,
		INDEXES,
		TOTAL
	}

	/// <summary>
	/// DBWorker does most of the work.  By default, it runs in its own thread.  Additionally,
	/// a few of the methods create another thread out of the threadpool, as to not interrupt this
	/// secondary thread.
	/// 
	/// There are a bunch of 'async' methods, which probably could be used directly within Unity
	/// using a coroutine.  Async methods pausing the thread just don't make much sense.
	/// 
	/// Each of the methods have callbacks if you choose to use them, either as a response with a boolean
	/// as to whether it worked or not, or with an object with the results.  I find it strange that the object
	/// you pass as the parameter cannot be updated, you have to return the results then apply the values.
	/// </summary>
	public class DBWorker
	{
		public DBConnect DBConnect { get; private set; }
		public static DBWorker Instance { get; private set; }
		public static Thread WorkerThread { get; private set; }

		const int MAX_RETRY = 30;
		StringBuilder sb = new StringBuilder();

		/// <summary>
		/// Creates the connection object
		/// Creates the worker object
		/// Creates thread for worker object to use
		/// Starts thread
		/// -- should be used before any other calls to this class
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="useProxy"></param>
		/// <param name="useLocalDynamoDB"></param>
		public static void InitializeForUnity(DBConnect connection, bool useProxy, bool useLocalDynamoDB)
		{
			if (Application.isPlaying)
			{
				if (useLocalDynamoDB)
					Debug.Log("DBWorker Initializing for Unity Startup using LOCAL DynamoDB");
				else
					Debug.Log("DBWorker Initializing for Unity Startup " + (useProxy ? "using proxy" : "WITHOUT proxy"));

				DBWorker worker = new DBWorker();
				worker.DBConnect = connection;
				Instance = worker;
				WorkerThread = new Thread(new ThreadStart(worker.Run));
				WorkerThread.Start();
			}
		}

		/// <summary>
		/// Main Loop, keeps it running in it's own thread so it will not block Main Unity Thread
		/// </summary>
		public void Run()
		{
			while (true)
			{
				Thread.Sleep(1);
			}
		}

		#region LOW-LEVEL .NET API

		/// <summary>
		/// PutItem will overwrite anything/everything if it already exists
		/// so make sure this is the desired action you are looking for
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="dbObject"></param>
		/// <param name="response"></param>
		public void PutItem(string tableName, DBObject dbObject, DDBCompletionDelegate response = null)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("PutItem is starting for hash=" + dbObject.GetHashKey() + " range=" + dbObject.GetRangeKey());

			PutItemRequest putRequest = new PutItemRequest
			{
				TableName = tableName,
				Item = dbObject.mAttributeValues
			};

			Debug.Log("--> " + putRequest.TableName);
			Debug.Log("--> " + putRequest.Item.Count);

			try
			{
				if (currentAttempt > 0)
				{
					Thread.Sleep(currentAttempt * 200);
				}

				lock (dbObject)
				{
					DBConnect.Client.PutItemAsync(putRequest, (result) =>
					{
						if (result.Exception != null)
						{
							if (DBUnityHelper.SHOW_DEBUG)
								DBTools.PrintException("PutItem", result.Exception);
							if (response != null)
								response(false, result.Exception);
							Debug.LogError(result.Exception.Message);
						}
						else
						{
							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("PutItem Has Completed.");
							if (response != null)
								response(true, null);
						}
						dbObject.mIsDirty = false;
					});
				}
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("PutItem", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else if (response != null)
					response(false, e);
			}
			catch (Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("PutItem", e);
				if (response != null)
					response(false, e);
			}
		}

		/// <summary>
		/// Will try to 'get' an item without any attributes, only to see whether the item exists or not
		/// Use callback to determine whether read object exists or not
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="hashKey"></param>
		/// <param name="hashItem"></param>
		/// <param name="rangeKey"></param>
		/// <param name="rangeItem"></param>
		/// <param name="response"></param>
		public void Exists(string tableName, string hashKey, string hashItem, string rangeKey, string rangeItem, DDBCompletionDelegate response)
		{
			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("Exists is starting for hash=" + hashItem + " range=" + rangeItem);

			GetItemRequest getRequest = new GetItemRequest
			{
				TableName = tableName,
				Key = new Dictionary<string, AttributeValue>()
				{
					{ hashKey, new AttributeValue() { S = hashItem } },
					{ rangeKey, new AttributeValue() { S = rangeItem } }
				},
				AttributesToGet = new List<string>()
			};
			Exists(getRequest, response);
		}

		/// <summary>
		/// Will try to 'get' an item without any attributes, only to see whether the item exists or not
		/// Use callback to determine whether read object exists or not
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="dbObject"></param>
		/// <param name="response"></param>
		public void Exists(string tableName, DBObject dbObject, DDBCompletionDelegate response)
		{
			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("Exists is starting for hash=" + dbObject.GetHashKey() + " range=" + dbObject.GetRangeKey());

			GetItemRequest getRequest = new GetItemRequest
			{
				TableName = tableName,
				Key = dbObject.mItemKeys,
				AttributesToGet = new List<string>() { "X" }
			};
			Exists(getRequest, response);
		}

		/// <summary>
		/// Will try to 'get' an item without any attributes, only to see whether the item exists or not
		/// Use callback to determine whether read object exists or not
		/// </summary>
		/// <param name="getRequest"></param>
		/// <param name="response"></param>
		public void Exists(GetItemRequest getRequest, DDBCompletionDelegate response)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}

				DBConnect.Client.GetItemAsync(getRequest, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("Exists", result.Exception);
						if (response != null)
							response(false, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						// so apparently the response is never null, just an empty set 
						if (result == null || result.Response == null || result.Response.Item.Count == 0)
						{
							if (DBUnityHelper.SHOW_DEBUG)
							{
								if (result == null)
									Debug.Log("Exists Response - getResponse is null or empty");
								else if (result.Response.Item == null)
									Debug.Log("Exists Response - getResponse.Item is null or empty");
								else if (result.Response.Item.Count == 0)
									Debug.Log("Exists Response - getResponse.Item.Count == 0");
							}

							if (response != null)
								response(false, null);
						}
						else
						{
							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("Exists Response - Items Read=" + result.Response.Item.Count);

							if (response != null)
								response(true, null);
						}
					}

					PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
				});
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Exists", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else
					response(false, e);
			}
			catch (Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Exists", e);
				response(false, e);
			}
		}

		/// <summary>
		/// Will read DDB Item Attributes as requested on the dbObject, as such
		/// it will not load the whole Item, only the requestd attributes.
		/// Creates a thread so it will not block while getting an item
		/// Use callback to interact with read object
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="dbObject"></param>
		/// <param name="response"></param>
		public void ReadItem(string tableName, DBObject dbObject, DDBReadObjDelegate response)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("ReadItem is starting for hash=" + dbObject.GetHashKey() + " range=" + dbObject.GetRangeKey());

			GetItemRequest getRequest = new GetItemRequest();
			getRequest.TableName = tableName;
			getRequest.Key = dbObject.mItemKeys;
			if (dbObject.mAttributesToGet != null && dbObject.mAttributesToGet.Count > 0)
				getRequest.AttributesToGet = dbObject.mAttributesToGet;

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}

				lock (dbObject)
				{
					DBConnect.Client.GetItemAsync(getRequest, (result) =>
					{
						if (result.Exception != null)
						{
							if (DBUnityHelper.SHOW_DEBUG)
								DBTools.PrintException("ReadItem", result.Exception);
							if (response != null)
								response(null, result.Exception);
							Debug.LogError(result.Exception.Message);
						}
						else
						{
							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("ReadItem EndGetItem - if failure, confirm table name is correct");

							// so apparently the response is never null, just an empty set 
							if (result == null || result.Response.Item == null || result.Response.Item.Count == 0)
							{
								if (DBUnityHelper.SHOW_DEBUG)
								{
									if (result == null)
										Debug.Log("ReadItem Response - getResponse is null or empty");
									else if (result.Response.Item == null)
										Debug.Log("ReadItem Response - getResponse.Item is null or empty");
									else if (result.Response.Item.Count == 0)
										Debug.Log("ReadItem Response - getResponse.Item.Count == 0");
								}

								if (response != null)
									response(null, null);
							}
							else
							{
								if (DBUnityHelper.SHOW_DEBUG)
								{
									Debug.Log("ReadItem Response - Items Read=" + result.Response.Item.Count);
									DBTools.PrintDictionaryValues(result.Response.Item);
								}

								dbObject.mAttributeValues = result.Response.Item;
								dbObject.mIsDirty = false;

								if (DBUnityHelper.SHOW_DEBUG)
									Debug.Log("ReadItem Has Completed.");

								if (response != null)
									response(dbObject, null);
							}
						}

						PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
					});
				}
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("ReadItem", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else
					response(null, e);
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("ReadItem", e);
				response(null, e);
			}
		}

		/// <summary>
		/// Updates the values on the database item
		/// If attribute is already there, this will overwrite it
		/// If attribute is to be removed, must be set on the object
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="dbObject"></param>
		/// <param name="response"></param>
		/// <param name="checkDirty"></param>
		public void UpdateItem(string tableName, DBObject dbObject, DDBCompletionDelegate response = null, bool checkDirty = false)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("UpdateItem is starting for hash=" + dbObject.GetHashKey() + " range=" + dbObject.GetRangeKey());

			if (!checkDirty || dbObject.mIsDirty)
			{

				UpdateItemRequest updateRequest = new UpdateItemRequest
				{
					TableName = tableName,
					Key = dbObject.mItemKeys,
					AttributeUpdates = dbObject.mAttributeUpdates
				};

				//if (DBUnityHelper.SHOW_DEBUG)
				//{
				//	Debug.Log("--> " + updateRequest.TableName);
				//	Debug.Log("--> " + updateRequest.Key.Count);
				//}

				try
				{
					if (currentAttempt > 0)
					{
						System.Threading.Thread.Sleep(currentAttempt * 200);
					}

					lock (dbObject)
					{
						DBConnect.Client.UpdateItemAsync(updateRequest, (result) =>
						{
							if (result.Exception != null)
							{
								if (DBUnityHelper.SHOW_DEBUG)
									DBTools.PrintException("UpdateItem", result.Exception);
								if (response != null)
									response(false, result.Exception);
								Debug.LogError(result.Exception.Message);
							}
							else
							{
								if (DBUnityHelper.SHOW_DEBUG)
									Debug.Log("UpdateItem EndGetItem - if failure, confirm table name is correct");

								if (result == null)
								{
									if (DBUnityHelper.SHOW_DEBUG)
										Debug.Log("UpdateItem Response - Item is null or empty");
								}
								else
								{
									if (DBUnityHelper.SHOW_DEBUG)
										Debug.Log("UpdateItem Response Successful");

									dbObject.mAttributeUpdates.Clear();
									dbObject.msgData = true;
									dbObject.mIsDirty = false;

									if (DBUnityHelper.SHOW_DEBUG)
										Debug.Log("UpdateItem Has Completed.");

									if (response != null)
										response(true, null);
								}
							}

							PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
						});
					}
				}
				catch (ProvisionedThroughputExceededException e)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						DBTools.PrintException("UpdateItem", e);
					currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
					if (++totalAttempts < MAX_RETRY)
						throw;
					else if (response != null)
						response(false, e);
				}
				catch (System.Exception e)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						DBTools.PrintException("UpdateItem", e);
					if (response != null)
						response(false, e);
				}
			}
		}

		/// <summary>
		/// Deletes an item (whole item, not attributes) permanently from the database
		/// Unlike others, this one will keep trying more quickly, and more often
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="dbObject"></param>
		/// <param name="response"></param>
		public void DeleteItem(string tableName, DBObject dbObject, DDBCompletionDelegate response = null)
		{
			bool retry = false;

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("DeleteItem is starting for hash=" + dbObject.GetHashKey() + " range=" + dbObject.GetRangeKey());

			DeleteItemRequest deleteRequest = new DeleteItemRequest
			{
				TableName = tableName,
				Key = dbObject.mItemKeys
			};

			try
			{
				if (retry)
				{
					retry = false;
					System.Threading.Thread.Sleep(200);
				}

				lock (dbObject)
				{
					DBConnect.Client.DeleteItemAsync(deleteRequest, (result) =>
					{
						if (result.Exception != null)
						{
							if (DBUnityHelper.SHOW_DEBUG)
								DBTools.PrintException("DeleteItem", result.Exception);
							if (response != null)
								response(false, result.Exception);
							Debug.LogError(result.Exception.Message);
						}
						else
						{
							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("DeleteItem EndPutItem - if failure, confirm table name is correct");

							// so apparently the response is never null, just an empty set 
							if (result == null)
							{
								if (DBUnityHelper.SHOW_DEBUG)
									Debug.Log("DeleteItem Response - Item is null or empty");
							}

							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("DeleteItem Has Completed.");

							if (response != null)
								response(true, null);
						}

						PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
					});
				}
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("DeleteItem", e);
				retry = true;
				throw;
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("DeleteItem", e);
				if (response != null)
					response(false, e);
			}
		}

		#endregion

		#region HIGHER-LEVEL .NET INTERFACES

		/// <summary>
		/// High-level Save method
		/// Created on own thread via Threadpool
		/// Save is multipurpose Add/Update/Create/Etc.  If it doesn't exist, it will create it.
		/// All values being saved must not be null, empty string, or empty set.  Deleting a value is not supported.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="response"></param>
		/// <param name="go"></param>
		/// <param name="nextMethod"></param>
		public void Save<T>(T obj, DDBCompletionResponseDelegate response = null, GameObject go = null, string nextMethod = null)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("Save DynamoDB Item Starting...");

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}
				AsyncOptions options = new AsyncOptions();
				options.ExecuteCallbackOnMainThread = false;

				DBConnect.Context.SaveAsync<T>(obj, (result) =>
				{
					Debug.Log("Save DynamoDB Item Completed!!! ");

					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("Save", result.Exception);
						if (response != null)
							response(false, go, nextMethod, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (DBUnityHelper.SHOW_DEBUG)
							Debug.Log("Save DynamoDB Item Completed...");
						if (response != null)
							response(true, go, nextMethod, null);
					}
				});
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Save", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else if (response != null)
					response(false, go, nextMethod, e);
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Save", e);
				if (response != null)
					response(false, go, nextMethod, e);
			}
		}

		/// <summary>
		/// High-level Save method
		/// Created on own thread via Threadpool
		/// Save is multipurpose Add/Update/Create/Etc.  If it doesn't exist, it will create it.
		/// All values being saved must not be null, empty string, or empty set.  Deleting a value is not supported.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="response"></param>
		/// <param name="go"></param>
		/// <param name="nextMethod"></param>
		public void SaveMono<T>(T obj, DDBCompletionResponseDelegateMono response = null, MonoBehaviour go = null, string nextMethod = null)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("Save DynamoDB Item Starting...");

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}

				DBConnect.Context.SaveAsync<T>(obj, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("Save", result.Exception);
						if (response != null)
							response(false, go, nextMethod, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (DBUnityHelper.SHOW_DEBUG)
							Debug.Log("Save DynamoDB Item Completed...");
						if (response != null)
							response(true, go, nextMethod, null);
					}
				});
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Save", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else if (response != null)
					response(false, go, nextMethod, e);
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Save", e);
				if (response != null)
					response(false, go, nextMethod, e);
			}
		}

		/// <summary>
		/// High-level Load Method, it is not asynchronous since it is spawned in a worker thread
		/// Will try a certain number of times before giving up (MAX_RETRY) if provisioned throughput not high enough
		/// Can capture the returned object in the callback
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="response"></param>
		/// <param name="go"></param>
		/// <param name="nextMethod"></param>
		public void Load<T>(T obj, DDBLoadObjResponseDelegate<T> response, GameObject go = null, string nextMethod = null)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("Load DynamoDB Item Starting...");

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}

				DBConnect.Context.LoadAsync<T>((T)obj, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("Load", result.Exception);
						if (response != null)
							response(default(T), go, nextMethod, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (result.Result == null)
						{
							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("Load DynamoDB Item Completed... response was NULL");
							response(default(T), go, nextMethod, null);
						}
						else
						{
							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("Load DynamoDB Item Completed... response was valid");
							response(result.Result, go, nextMethod, null);
						}
					}
				}, null);
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Load", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else
					response(default(T), go, nextMethod, e);
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Load", e);
				response(default(T), go, nextMethod, e);
			}
		}

		/// <summary>
		/// High-level Load Method, it is not asynchronous since it is spawned in a worker thread
		/// Will try a certain number of times before giving up (MAX_RETRY) if provisioned throughput not high enough
		/// Can capture the returned object in the callback
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="hashKey"></param>
		/// <param name="rangeKey"></param>
		/// <param name="response"></param>
		/// <param name="go"></param>
		/// <param name="nextMethod"></param>
		public void Load<T>(string hashKey, string rangeKey, DDBLoadObjResponseDelegate<T> response, GameObject go = null, string nextMethod = null)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("Load DynamoDB Item Starting...");

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}

				DBConnect.Context.LoadAsync<T>(hashKey, rangeKey, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("Load", result.Exception);
						if (response != null)
							response(default(T), go, nextMethod, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (result.Result == null)
						{
							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("Load DynamoDB Item Completed... response was NULL");
							response(default(T), go, nextMethod, null);
						}
						else
						{
							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("Load DynamoDB Item Completed... response was valid");
							response(result.Result, go, nextMethod, null);
						}
					}
				});
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Load", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else
					response(default(T), go, nextMethod, e);
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Load", e);
				response(default(T), go, nextMethod, e);
			}
		}

		/// <summary>
		/// High-level Load Method, it is not asynchronous since it is spawned in a worker thread
		/// Will try a certain number of times before giving up (MAX_RETRY) if provisioned throughput not high enough
		/// Can capture the returned object in the callback
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="response"></param>
		/// <param name="go"></param>
		/// <param name="nextMethod"></param>
		public void LoadMono<T>(T obj, DDBLoadObjResponseDelegateMono<T> response, MonoBehaviour go = null, string nextMethod = null)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("Load DynamoDB Item Starting...");

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}

				DBConnect.Context.LoadAsync<T>(obj, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("Load", result.Exception);
						if (response != null)
							response(default(T), go, nextMethod, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (result.Result == null)
						{
							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("Load DynamoDB Item Completed... response was NULL");
							response(default(T), go, nextMethod, null);
						}
						else
						{
							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("Load DynamoDB Item Completed... response was valid");
							response(result.Result, go, nextMethod, null);
						}
					}
				}, null);
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Load", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else
					response(default(T), go, nextMethod, e);
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Load", e);
				response(default(T), go, nextMethod, e);
			}
		}

		/// <summary>
		/// High-level Load Method, it is not asynchronous since it is spawned in a worker thread
		/// Will try a certain number of times before giving up (MAX_RETRY) if provisioned throughput not high enough
		/// Can capture the returned object in the callback
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="hashKey"></param>
		/// <param name="rangeKey"></param>
		/// <param name="response"></param>
		/// <param name="go"></param>
		/// <param name="nextMethod"></param>
		public void LoadMono<T>(string hashKey, string rangeKey, DDBLoadObjResponseDelegateMono<T> response, MonoBehaviour go = null, string nextMethod = null)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("Load DynamoDB Item Starting...");

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}

				DBConnect.Context.LoadAsync<T>(hashKey, rangeKey, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("Load", result.Exception);
						if (response != null)
							response(default(T), go, nextMethod, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (result.Result == null)
						{
							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("Load DynamoDB Item Completed... response was NULL");
							response(default(T), go, nextMethod, null);
						}
						else
						{
							if (DBUnityHelper.SHOW_DEBUG)
								Debug.Log("Load DynamoDB Item Completed... response was valid");
							response(result.Result, go, nextMethod, null);
						}
					}
				}, null);
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Load", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else
					response(default(T), go, nextMethod, e);
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Load", e);
				response(default(T), go, nextMethod, e);
			}
		}

		/// <summary>
		/// High-level delete method
		/// Give it an object, it will delete it
		/// async method with infinite retries if provisioned throughput fails
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="response"></param>
		/// <param name="go"></param>
		/// <param name="nextMethod"></param>
		public void Delete<T>(T obj, DDBCompletionResponseDelegate response = null, GameObject go = null, string nextMethod = null)
		{
			bool retry = false;

			try
			{
				if (retry)
				{
					retry = false;
					System.Threading.Thread.Sleep(200);
				}

				DBConnect.Context.DeleteAsync<T>(obj, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("Delete", result.Exception);
						if (response != null)
							response(false, go, nextMethod, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (DBUnityHelper.SHOW_DEBUG)
							Debug.Log("Delete DynamoDB Item Completed");
						if (response != null)
							response(true, go, nextMethod, null);
					}
				}, null);
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Delete", e);
				retry = true;
				throw;
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("Delete", e);
				if (response != null)
					response(false, go, nextMethod, e);
			}
		}

		/// <summary>
		/// Threaded Query for getting specific attributes back on an item. 
		/// Whatever attributes keys you provide in the string list will be the values returned.  Case is important, as always
		/// Object is returned as a DBObject so getting the values form the returned attributes is easier
		/// Like other methods, this will return up to a specific point of failure
		/// This will evaluate only up to a _single_ item and return a _single_ item..  
		/// The equality operator can be changed to any of enumeration types
		/// http://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_Query.html
		/// 
		/// Consistent read boolean is whether or not have the query use "eventual consistent read" vs. "strongly consistent read"
		/// Strongly consistent reads will return the most up-to-date information; however use more capacity.  Eventual consistent reads
		/// may be off by a second or two and return stale data, which may be fine in some instances.
		/// http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/APISummary.html
		/// 
		/// Note: Query return size maximum is 1 MB
		/// </summary>
		/// <param name="hashItem"></param>
		/// <param name="rangeItem"></param>
		/// <param name="queryType"></param>
		/// <param name="attibutesToGet"></param>
		/// <param name="response"></param>
		/// <param name="consistentRead"></param>
		public void QuerySingle(string tableName, string hashItem, string rangeItem, DBTools.QueryOperator queryType, List<string> attibutesToGet,
			DDBReadObjDelegate response, bool consistentRead = false)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			// note that I keep all hashKeys lower case.  Adjust as needed
			hashItem = hashItem.ToLower();

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("QuerySingle DynamoDB Attibutes Starting... for " + hashItem + ":" + rangeItem);

			QueryRequest queryRequest = new QueryRequest();
			queryRequest.Limit = 1;
			queryRequest.TableName = tableName;
			queryRequest.ConsistentRead = consistentRead;
			queryRequest.AttributesToGet = attibutesToGet;
			queryRequest.KeyConditions = new Dictionary<string, Condition>()
			{
				{
					// hash keys can -only- be equality operator, per DynamoDB requirements
					DBObject.HASH_KEY,
					new Condition()
					{
						ComparisonOperator = "EQ",
						AttributeValueList = new List<AttributeValue>() { new AttributeValue { S = hashItem } }
					}
				},
				{
					DBObject.RANGE_KEY,
					new Condition()
					{
						ComparisonOperator = DBTools.ConvertQueryOperator(queryType),
						AttributeValueList = new List<AttributeValue>() { new AttributeValue { S = rangeItem } }
					}
				}
					};

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}

				DBConnect.Client.QueryAsync(queryRequest, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("QuerySingle", result.Exception);
						if (response != null)
							response(null, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (DBUnityHelper.SHOW_DEBUG)
						{
							if (result == null || result.Response == null)
							{
								Debug.Log("QuerySingle DynamoDB response was NULL");
							}
							else
							{
								Debug.Log("QuerySingle DynamoDB response len=" + result.Response.Items.Count);
								foreach (var item in result.Response.Items)
									DBTools.PrintDictionaryValues(item);
							}
						}

						if (result.Response != null)
						{
							if (result.Response.Items.Count > 0)
								response(new DBObject(hashItem, rangeItem) { mAttributeValues = result.Response.Items[0] }, null);
							else
								response(null, null);
						}
					}

					PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
				});
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("QuerySingle", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else
					response(null, e);
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("QuerySingle", e);
				response(null, e);
			}
		}

		/// <summary>
		/// Overloaded version, using Equality for deciding query type as default
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="hashItem"></param>
		/// <param name="rangeItem"></param>
		/// <param name="attibutesToGet"></param>
		/// <param name="response"></param>
		/// <param name="consistentRead"></param>
		public void QuerySingle(string tableName, string hashItem, string rangeItem, List<string> attibutesToGet,
			DDBReadObjDelegate response, bool consistentRead = false)
		{
			QuerySingle(tableName, hashItem, rangeItem, DBTools.QueryOperator.Equal, attibutesToGet, response, consistentRead);
		}

		/// <summary>
		/// Threaded Query for checking whether a specific HashKey Exists
		/// Whatever attributes keys you provide in the string list will be the values returned.  Case is important, as always
		/// Object is returned as a DBObject so getting the values form the returned attributes is easier
		/// Like other methods, this will return up to a specific point of failure
		/// This will evaluate only up to a _single_ item and return a _single_ item..  
		/// The equality operator can be changed to any of enumeration types
		/// http://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_Query.html
		/// </summary>
		/// <param name="hashItem"></param>
		/// <param name="attibutesToGet"></param>
		/// <param name="response"></param>
		/// <param name="consistentRead"></param>
		public void QueryHashKeyOnly(
			string tableName,
			string hashItem,
			List<string> attibutesToGet,
			DDBCompletionDelegate response,
			bool consistentRead = false)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			// note that I keep all hashKeys lower case.  Adjust as needed
			hashItem = hashItem.ToLower();

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("QueryHashKeyOnly DynamoDB Attibutes Starting... for " + hashItem);

			QueryRequest queryRequest = new QueryRequest();
			queryRequest.Limit = 1;
			queryRequest.TableName = tableName;
			queryRequest.ConsistentRead = consistentRead;
			queryRequest.AttributesToGet = attibutesToGet;
			queryRequest.KeyConditions = new Dictionary<string, Condition>()
			{
				{
					DBObject.HASH_KEY,
					new Condition()
					{
						ComparisonOperator = "EQ",
						AttributeValueList = new List<AttributeValue>() { new AttributeValue { S = hashItem } }
					}
				}
			};

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}

				DBConnect.Client.QueryAsync(queryRequest, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("QueryHashKeyOnly", result.Exception);
						if (response != null)
							response(false, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (DBUnityHelper.SHOW_DEBUG)
						{
							Debug.Log("QueryHashKeyOnly DynamoDB response len=" + result.Response.Items.Count);
							foreach (var item in result.Response.Items)
								DBTools.PrintDictionaryValues(item);
						}

						if (response != null)
						{
							if (result.Response != null && result.Response.Items.Count > 0)
								response(true, null);
							else
								response(false, null);
						}
					}

					PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
				});
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("QueryHashKeyOnly", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else
					response(false, e);
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("QueryHashKeyOnly", e);
				response(false, e);
			}
		}

		/// <summary>
		/// Threaded Query for getting specific attributes back on items from HashKey 
		/// Whatever attributes keys you provide in the string list will be the values returned.  Case is important, as always
		/// Object is returned as a DBObject so getting the values form the returned attributes is easier
		/// Like other methods, this will return up to a specific point of failure
		/// This will evaluate only up to a _single_ item and return a _single_ item..  
		/// The equality operator can be changed to any of enumeration types
		/// http://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_Query.html
		/// </summary>
		/// <param name="hashItem"></param>
		/// <param name="attibutesToGet"></param>
		/// <param name="response"></param>
		/// <param name="consistentRead"></param>
		public void QueryHashKeyOnly(string tableName, string hashItem, List<string> attibutesToGet,
			DDBQueryHashKeyOnlyDelegate response, int itemLimit = 10, bool consistentRead = false)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			// note that I keep all hashKeys lower case.  Adjust as needed
			hashItem = hashItem.ToLower();

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("QueryHashKeyOnly DynamoDB Attibutes Starting... for " + hashItem);

			QueryRequest queryRequest = new QueryRequest();
			queryRequest.Limit = itemLimit;
			queryRequest.TableName = tableName;
			queryRequest.ConsistentRead = consistentRead;
			queryRequest.AttributesToGet = attibutesToGet;
			queryRequest.KeyConditions = new Dictionary<string, Condition>()
			{
				{
					DBObject.HASH_KEY,
					new Condition()
					{
						ComparisonOperator = "EQ",
						AttributeValueList = new List<AttributeValue>() { new AttributeValue { S = hashItem } }
					}
				}
			};

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}

				DBConnect.Client.QueryAsync(queryRequest, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("QueryHashKeyOnly", result.Exception);

						if (response != null)
							response(null, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (DBUnityHelper.SHOW_DEBUG)
						{
							Debug.Log("QueryHashKeyOnly DynamoDB response len=" + result.Response.Items.Count);
							foreach (var item in result.Response.Items)
								DBTools.PrintDictionaryValues(item);
						}

						if (response != null)
						{
							if (result.Response != null && result.Response.Items.Count > 0)
								response(result.Response.Items, null);
						}
					}

					PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
				});
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("QueryHashKeyOnly", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else
					response(null, e);
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("QueryHashKeyOnly", e);
				response(null, e);
			}
		}

		/// <summary>
		/// Threaded Query for getting Items (whole item) back from a query
		/// It knows which Table to use from the tablename of the Type T table definition, which is why it is not set here.
		/// 
		/// Whatever attributes keys you provide in the string list will be the values returned.  Case is important, as always
		/// List is returned filled with all matching Items of Type T
		/// Like other methods, this will return up to a specific point of failure
		/// This will evaluate all items that match and return them accordingly
		/// </summary>
		/// <param name="hashKey"></param>
		/// <param name="attibutesToGet"></param>
		/// <param name="response"></param>
		/// <param name="consistentRead"></param>
		public void QueryHashKeyObject<T>(string hashKey, List<string> attibutesToGet, DDBQueryHashKeyOnlyDelegate<T> response, bool consistentRead = true)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			// note that I keep all hashKeys lower case.  Adjust as needed
			hashKey = hashKey.ToLower();

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("QueryHashKeyOnly DynamoDB Attibutes Starting... for " + hashKey);

			DynamoDBOperationConfig config = new DynamoDBOperationConfig();
			config.ConsistentRead = consistentRead;

			ThreadPool.QueueUserWorkItem(delegate
			{
				try
				{
					if (currentAttempt > 0)
					{
						System.Threading.Thread.Sleep(currentAttempt * 200);
					}

					var search = DBConnect.Context.QueryAsync<T>(hashKey, config);

					search.GetRemainingAsync((callback) =>
					{
						if (callback.Exception != null)
						{
							if (DBUnityHelper.SHOW_DEBUG)
								DBTools.PrintException("QueryHashKeyOnly", callback.Exception);

							if (response != null)
								response(null, callback.Exception);
							Debug.LogError(callback.Exception.Message);
						}
						else
						{
							if (DBUnityHelper.SHOW_DEBUG)
							{
								Debug.Log("QueryHashKeyOnly DynamoDB response len=" + callback.Result.Count);
								foreach (var item in callback.Result)
									Debug.Log("Item Found -- " + item.ToString());
							}

							if (response != null)
							{
								if (callback.Result != null && callback.Result.Count > 0)
									response(callback.Result, null);
							}
						}
					});
				}
				catch (ProvisionedThroughputExceededException e)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						DBTools.PrintException("QueryHashKeyOnly", e);
					currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
					if (++totalAttempts < MAX_RETRY)
						throw;
					else
						response(null, e);
				}
				catch (System.Exception e)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						DBTools.PrintException("QueryHashKeyOnly", e);
					response(null, e);
				}
			});
		}

		/// <summary>
		/// Threaded Query for getting Items (whole item) back from a query
		/// It knows which Table to use from the tablename of the Type T table definition, which is why it is not set here.
		/// 
		/// for all operations except QueryOperator.Between, values should be one value.
		/// for QueryIperator.Between, values should be two values
		///
		/// Whatever attributes keys you provide in the string list will be the values returned.  Case is important, as always
		/// List is returned filled with all matching Items of Type T
		/// Like other methods, this will return up to a specific point of failure
		/// This will evaluate all items that match and return them accordingly
		/// </summary>
		/// <param name="hashKey"></param>
		/// <param name="op"></param>
		/// <param name="rangeKey"></param>
		/// <param name="attibutesToGet"></param>
		/// <param name="response"></param>
		/// <param name="consistentRead"></param>
		public void QueryObject<T>(string hashKey, QueryOperator op, IEnumerable<object> values,
			List<string> attibutesToGet, DDBQueryHashKeyOnlyDelegate<T> response, bool consistentRead = true)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			// note that I keep all hashKeys lower case.  Adjust as needed
			hashKey = hashKey.ToLower();

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("QueryObject DynamoDB Attibutes Starting... for " + hashKey);

			DynamoDBOperationConfig config = new DynamoDBOperationConfig();
			config.ConsistentRead = consistentRead;

			ThreadPool.QueueUserWorkItem(delegate
			{
				try
				{
					if (currentAttempt > 0)
					{
						System.Threading.Thread.Sleep(currentAttempt * 200);
					}

					// for all operations except QueryOperator.Between, values should be one value.
					// for QueryIperator.Between, values should be two values
					var search = DBConnect.Context.QueryAsync<T>(hashKey, op, values, config);

					search.GetRemainingAsync((callback) =>
					{
						if (callback.Exception != null)
						{
							if (DBUnityHelper.SHOW_DEBUG)
								DBTools.PrintException("QueryObject", callback.Exception);

							if (response != null)
								response(null, callback.Exception);
							Debug.LogError(callback.Exception.Message);
						}
						else
						{
							if (DBUnityHelper.SHOW_DEBUG)
							{
								Debug.Log("QueryObject DynamoDB response len=" + callback.Result.Count);
								foreach (var item in callback.Result)
									Debug.Log("Item Found -- " + item.ToString());
							}

							if (response != null)
							{
								if (callback.Result != null && callback.Result.Count > 0)
									response(callback.Result, null);
							}
						}
					});
				}
				catch (ProvisionedThroughputExceededException e)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						DBTools.PrintException("QueryObject", e);
					currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
					if (++totalAttempts < MAX_RETRY)
						throw;
					else
						response(null, e);
				}
				catch (System.Exception e)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						DBTools.PrintException("QueryObject", e);
					response(null, e);
				}
			});
		}
		
		/// <summary>
		/// Similar to QuerySingle; however, this one uses a secondary index for results
		/// 
		/// It works like...
		/// hashItem is the DynamoDB hashItem you are trying to query for
		/// rangeItem is the additional value you are trying to query for, so these two are what you're trying to find
		///     even though it's called "rangeItem" it's just the second half of the pair you're querying for
		/// 
		/// secondary index key and secondary index name - these are the names of the key/index you created in DynamoDB
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="hashItem"></param>
		/// <param name="rangeItem"></param>
		/// <param name="secondaryIndexName"></param>
		/// <param name="secondaryIndexKey"></param>
		/// <param name="queryType"></param>
		/// <param name="attibutesToGet"></param>
		/// <param name="response"></param>
		/// <param name="consistentRead"></param>
		public void QuerySingleSecondaryIndex(string tableName, string hashItem,  string rangeItem, string secondaryIndexName, 
			string secondaryIndexKey, DBTools.QueryOperator queryType, List<string> attibutesToGet, DDBReadObjDelegate response, bool consistentRead = false)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			// note that I keep all hashKeys lower case.  Adjust as needed
			hashItem = hashItem.ToLower();

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("QuerySingleSecondaryIndex Starting for " + hashItem + ":" + rangeItem + " SecondaryIndexName=" + secondaryIndexName + " SecondaryIndexKey=" + secondaryIndexKey);

			QueryRequest queryRequest = new QueryRequest();
			queryRequest.Limit = 1;
			queryRequest.TableName = tableName;
			queryRequest.ConsistentRead = consistentRead;
			queryRequest.AttributesToGet = attibutesToGet;
			queryRequest.IndexName = secondaryIndexName;
			queryRequest.KeyConditions = new Dictionary<string, Condition>()
			{
				{
					// hash keys can -only- be equality operator, per DynamoDB requirements
					DBObject.HASH_KEY,
					new Condition()
					{
						ComparisonOperator = "EQ",
						AttributeValueList = new List<AttributeValue>() { new AttributeValue { S = hashItem } }
					}
				},
				{
					secondaryIndexKey,
					new Condition()
					{
						ComparisonOperator = DBTools.ConvertQueryOperator(queryType),
						AttributeValueList = new List<AttributeValue>() { new AttributeValue { S = rangeItem } }
					}
				}
			};

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}

				DBConnect.Client.QueryAsync(queryRequest, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("QuerySingleSecondaryIndex", result.Exception);
						if (response != null)
							response(null, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (result == null || result.Response == null)
						{
							Debug.Log("QuerySingleSecondaryIndex DynamoDB response was NULL");
						}
						else
						{
							Debug.Log("QuerySingleSecondaryIndex DynamoDB response len=" + result.Response.Items.Count);
							foreach (var item in result.Response.Items)
								DBTools.PrintDictionaryValues(item);
						}

						if (result.Response != null && result.Response.Items.Count > 0)
							response(new DBObject(hashItem, rangeItem) { mAttributeValues = result.Response.Items[0] }, null);
						else
							response(null, null);
					}

					PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
				});

			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("QuerySingleSecondaryIndex", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else
					response(null, e);
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("QuerySingleSecondaryIndex", e);
				response(null, e);
			}
		}

		/// <summary>
		/// Threaded Query for getting specific attributes back on an item. 
		/// Whatever attributes keys you provide in the string list will be the values returned.  Case is important, as always
		/// Object is returned as a DBObject so getting the values form the returned attributes is easier
		/// Like other methods, this will return up to a specific point of failure
		/// This will evaluate only up to the requested number of items and return all of the items.  
		/// The equality operator can be changed to any of enumeration types
		/// http://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_Query.html
		/// 
		/// Consistent read boolean is whether or not have the query use "eventual consistent read" vs. "strongly consistent read"
		/// Strongly consistent reads will return the most up-to-date information; however use more capacity.  Eventual consistent reads
		/// may be off by a second or two and return stale data, which may be fine in some instances.
		/// http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/APISummary.html
		/// 
		/// Note: Query return size maximum is 1 MB
		/// </summary>
		/// <param name="hashItem"></param>
		/// <param name="rangeItem"></param>
		/// <param name="queryType"></param>
		/// <param name="requestLimit"></param>
		/// <param name="attibutesToGet"></param>
		/// <param name="response"></param>
		/// <param name="consistentRead"></param>
		public void QueryMultiple(string tableName, string hashItem, string rangeItem, DBTools.QueryOperator queryType, 
			int requestLimit, List<string> attibutesToGet, DDBReadObjDelegate response, bool consistentRead = false)
		{
			int currentAttempt = 0;
			int totalAttempts = 0;
			int maxAttempt = 5;

			// note that I keep all hashKeys lower case.  Adjust as needed
			hashItem = hashItem.ToLower();

			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("QueryMultiple DynamoDB Attibutes Starting... for " + hashItem + ":" + rangeItem);

			QueryRequest queryRequest = new QueryRequest();
			queryRequest.Limit = requestLimit;
			queryRequest.TableName = tableName;
			queryRequest.ConsistentRead = consistentRead;
			queryRequest.AttributesToGet = attibutesToGet;
			queryRequest.KeyConditions = new Dictionary<string, Condition>()
			{
				{
					// hash keys can -only- be equality operator, per DynamoDB requirements
					DBObject.HASH_KEY,
					new Condition()
					{
						ComparisonOperator = "EQ",
						AttributeValueList = new List<AttributeValue>() { new AttributeValue { S = hashItem } }
					}
				},
				{
					DBObject.RANGE_KEY,
					new Condition()
					{
						ComparisonOperator = DBTools.ConvertQueryOperator(queryType),
						AttributeValueList = new List<AttributeValue>() { new AttributeValue { S = rangeItem } }
					}
				}
			};

			try
			{
				if (currentAttempt > 0)
				{
					System.Threading.Thread.Sleep(currentAttempt * 200);
				}

				DBConnect.Client.QueryAsync(queryRequest, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("QueryMultiple", result.Exception);
						if (response != null)
							response(null, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (result == null || result.Response == null)
						{
							Debug.Log("QueryMultiple DynamoDB response was NULL");
						}
						else
						{
							Debug.Log("QueryMultiple DynamoDB response len=" + result.Response.Items.Count);
							foreach (var item in result.Response.Items)
								DBTools.PrintDictionaryValues(item);
						}

						if (result.Response != null)
						{
							if (result.Response.Items.Count > 0)
								response(new DBObject(hashItem, rangeItem) { mAttributeValues = result.Response.Items[0] }, null);
							else
								response(null, null);
						}
					}

					PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
				});
			}
			catch (ProvisionedThroughputExceededException e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("QueryMultiple", e);
				currentAttempt = (currentAttempt >= maxAttempt ? maxAttempt : currentAttempt + 1);
				if (++totalAttempts < MAX_RETRY)
					throw;
				else
					response(null, e);
			}
			catch (System.Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					DBTools.PrintException("QueryMultiple", e);
				response(null, e);
			}
		}

		/// <summary>
		/// Overloaded version, using Equality for deciding query type as default
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="hashItem"></param>
		/// <param name="rangeItem"></param>
		/// <param name="requestLimit"></param>
		/// <param name="attibutesToGet"></param>
		/// <param name="response"></param>
		/// <param name="consistentRead"></param>
		public void QueryMultiple(string tableName, string hashItem, string rangeItem, int requestLimit, List<string> attibutesToGet,
			DDBReadObjDelegate response, bool consistentRead = false)
		{
			QueryMultiple(tableName, hashItem, rangeItem, DBTools.QueryOperator.Equal, requestLimit, attibutesToGet, response, consistentRead);
		}
		#endregion

		/// <summary>
		/// Creates the table request, not really used externally as this doesn't process the request
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="hashKey"></param>
		/// <param name="hashType"></param>
		/// <param name="rangeKey"></param>
		/// <param name="rangeType"></param>
		/// <param name="readCapacity"></param>
		/// <param name="writeCapacity"></param>
		/// <param name="localSecondaryIndexes"></param>
		/// <param name="localSecondaryIndexTypes"></param>
		/// <returns></returns>
		private CreateTableRequest CreateTableRequest(
			string tableName,
			string hashKey, AttribType hashType,
			string rangeKey, AttribType rangeType,
			int readCapacity, int writeCapacity,
			List<LocalSecondaryIndex> localSecondaryIndexes = null,
			List<AttribType> localSecondaryIndexTypes = null)
		{
			CreateTableRequest request = new CreateTableRequest();
			request.TableName = tableName;

			// primary key schema - HASH and RANGE
			List<KeySchemaElement> indexKeySchema = new List<KeySchemaElement>();
			indexKeySchema.Add(new KeySchemaElement() { AttributeName = hashKey, KeyType = "HASH" });
			indexKeySchema.Add(new KeySchemaElement() { AttributeName = rangeKey, KeyType = "RANGE" });
			request.KeySchema = indexKeySchema;

			// attributes - must include any index names
			List<AttributeDefinition> attributeDefinitions = new List<AttributeDefinition>();
			attributeDefinitions.Add(new AttributeDefinition { AttributeName = hashKey, AttributeType = hashType.ToString() });
			attributeDefinitions.Add(new AttributeDefinition { AttributeName = rangeKey, AttributeType = rangeType.ToString() });

			// we build the index names from the localSecondaryIndex if available
			if (localSecondaryIndexes != null && localSecondaryIndexes.Count > 0)
			{
				int len = localSecondaryIndexes.Count;
				int schemaLen;
				for (int i = 0; i < len; i++)
				{
					schemaLen = localSecondaryIndexes[i].KeySchema.Count;
					for (int j = 0; j < schemaLen; j++)
					{
						if (localSecondaryIndexes[i].KeySchema[j].KeyType == "RANGE")
						{
							Debug.Log(">>>> " + localSecondaryIndexes[i].KeySchema[j].AttributeName);
							Debug.Log(">>>> " + localSecondaryIndexTypes[i].ToString());
							attributeDefinitions.Add(
								new AttributeDefinition
								{
									AttributeName = localSecondaryIndexes[i].KeySchema[j].AttributeName,
									AttributeType = localSecondaryIndexTypes[i].ToString()
								});
						}
					}
				}

				request.LocalSecondaryIndexes = localSecondaryIndexes;
			}

			// add attribute definitions now that the secondaryIndexes have been processed
			request.AttributeDefinitions = attributeDefinitions;

			// provisioned throughput
			request.ProvisionedThroughput = new ProvisionedThroughput
			{
				ReadCapacityUnits = readCapacity,
				WriteCapacityUnits = writeCapacity
			};

			return request;
		}

		/// <summary>
		/// Creates a single secondary index.  Used with DoCreateLocalSecondaryIndexes to create the list needed
		/// for create table.
		/// </summary>
		/// <param name="hashKey"></param>
		/// <param name="indexName"></param>
		/// <param name="attribName"></param>
		/// <param name="nonKeyAttributes"></param>
		/// <param name="projectionType"></param>
		/// <returns></returns>
		public LocalSecondaryIndex CreateLocalSecondaryIndex(string hashKey, string indexName, string attribName,
			List<string> nonKeyAttributes, ProjectionType projectionType)
		{
			List<KeySchemaElement> indexKeySchema = new List<KeySchemaElement>();
			indexKeySchema.Add(new KeySchemaElement() { AttributeName = hashKey, KeyType = "HASH" });
			indexKeySchema.Add(new KeySchemaElement() { AttributeName = attribName, KeyType = "RANGE" });

			Projection projection = new Projection();
			projection.ProjectionType = projectionType;
			projection.NonKeyAttributes = nonKeyAttributes;

			LocalSecondaryIndex localIndex = new LocalSecondaryIndex()
			{
				IndexName = indexName,
				KeySchema = indexKeySchema,
				Projection = projection
			};

			return localIndex;
		}

		/// <summary>
		/// Builds the list of secondary indexes
		/// This is what you'd use to build the secondary index lists used with CreateTable
		/// </summary>
		/// <param name="hashKey"></param>
		/// <param name="indexNames"></param>
		/// <param name="attribNames"></param>
		/// <param name="nonKeyAttribs"></param>
		/// <param name="projectionTpes"></param>
		/// <returns></returns>
		public List<LocalSecondaryIndex> DoCreateLocalSecondaryIndexes(string hashKey, List<string> indexNames,
			List<string> attribNames, List<List<string>> nonKeyAttribs, List<ProjectionType> projectionTpes)
		{
			List<LocalSecondaryIndex> localSecondaryIndexes = new List<LocalSecondaryIndex>();
			int len = indexNames.Count;

			for (int i = 0; i < len; i++)
				localSecondaryIndexes.Add(CreateLocalSecondaryIndex(hashKey, indexNames[i], attribNames[i], nonKeyAttribs[i], projectionTpes[i]));

			return localSecondaryIndexes;
		}

		/// <summary>
		/// 
		/// Create a table.  Do not try to access table until it has been created
		/// ---------------------and-------------------------
		/// is active.
		/// 
		/// if you want to use secondary indexes, you need to build them and send them as a parameter
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="hashKey"></param>
		/// <param name="hashType"></param>
		/// <param name="rangeKey"></param>
		/// <param name="rangeType"></param>
		/// <param name="readCapacity"></param>
		/// <param name="writeCapacity"></param>
		/// <param name="localSecondaryIndexes"></param>
		/// <param name="localSecondaryIndexTypes"></param>
		/// <param name="response"></param>
		public void CreateTable(string tableName, string hashKey, AttribType hashType, string rangeKey, AttribType rangeType,
			int readCapacity, int writeCapacity, List<LocalSecondaryIndex> localSecondaryIndexes = null,
			List<AttribType> localSecondaryIndexTypes = null, DDBCompletionDelegate response = null)
		{
			try
			{
				CreateTableRequest request = CreateTableRequest(tableName, hashKey, hashType, rangeKey, rangeType,
					readCapacity, writeCapacity, localSecondaryIndexes, localSecondaryIndexTypes);
				DBConnect.Client.CreateTableAsync(request, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("CreateTable", result.Exception);
						if (response != null)
							response(false, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						TableDescription tableDescription = result.Response.TableDescription;

						Debug.Log(tableDescription.TableName + ": " + tableDescription.TableStatus
							+ "\t ReadCapacityUnits: " + tableDescription.ProvisionedThroughput.ReadCapacityUnits
							+ "\t WriteCapacityUnits: " + tableDescription.ProvisionedThroughput.WriteCapacityUnits);

						string status = tableDescription.TableStatus;
						Console.WriteLine(" - " + status);

						if (DBUnityHelper.SHOW_DEBUG)
							Debug.Log("ProcessCreateTable Has Completed.");

						if (response != null)
							response(true, null);
					}

					PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
				});
			}
			catch (Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("Exception - CreateTable: " + e.Message);
				if (response != null)
					response(false, e);
			}
		}

		/// <summary>
		/// Deletes a table then informs you if it completed or not
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="response"></param>
		public void DeleteTable(string tableName, DDBCompletionDelegate response = null)
		{
			try
			{
				DeleteTableRequest request = new DeleteTableRequest { TableName = tableName };
				DBConnect.Client.DeleteTableAsync(request, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("DeleteTable", result.Exception);
						if (response != null)
							response(false, result.Exception);
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						if (DBUnityHelper.SHOW_DEBUG)
							Debug.Log("DeleteTable Complete, response status code = " + result.Response.HttpStatusCode);

						if (response != null)
							response(true, null);
					}

					PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
				});
			}
			catch (Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("Exception - DeleteTable: " + e.Message);
				if (response != null)
					response(false, e);
			}
		}

		/// <summary>
		/// Lists all tables in your DB
		/// </summary>
		/// <returns></returns>
		public string ListTables()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			// Initial value for the first page of table names.
			string lastEvaluatedTableName = null;

			// Create a request object to specify optional parameters.
			ListTablesRequest request = new ListTablesRequest
			{
				Limit = 10, // Page size.
				ExclusiveStartTableName = lastEvaluatedTableName
			};

			DBConnect.Client.ListTablesAsync(request, (result) =>
			{
				if (result.Exception != null)
				{
					sb.Append(result.Exception.Message);
				}
				else if (result.Response.TableNames.Count == 0)
				{
					sb.AppendLine("No Tables Found");
				}
				else
				{
					sb.AppendLine("List Table Names: ");
					var response = result.Response;
					foreach (string name in response.TableNames)
						sb.AppendLine(name);

					// repeat request to fetch more results
					lastEvaluatedTableName = response.LastEvaluatedTableName;
				}

				PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
			});
			return sb.ToString();
		}

		/// <summary>
		/// Basic describe table method, returns information about the table and prints it
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public void DescribeTable(string tableName, DDBDescribeTableDelegate response)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.AppendLine("Describe Table");

			DescribeTableRequest request = new DescribeTableRequest();
			request.TableName = tableName;

			DBConnect.Client.DescribeTableAsync(request, (result) =>
			{
				if (result.Exception != null)
				{
					sb.Append(result.Exception.Message);
				}
				else
				{
					TableDescription desc = result.Response.Table;

					sb.AppendLine("Created = " + desc.CreationDateTime);
					sb.AppendLine("Status = " + desc.TableStatus);
					sb.AppendLine("ItemCount = " + desc.ItemCount);
					sb.AppendLine("Bytes = " + desc.TableSizeBytes);

					foreach (var item in desc.AttributeDefinitions)
						sb.AppendLine("Attribute Definitions = " + item.AttributeName + " " + item.AttributeType.Value);

					foreach (var item in desc.KeySchema)
						sb.AppendLine("Schema = " + item.AttributeName + " " + item.KeyType.Value);

					foreach (var item in desc.LocalSecondaryIndexes)
					{
						sb.AppendLine("SecondaryIndex = " + item.IndexName);
						foreach (var ind in item.KeySchema)
						{
							sb.AppendLine("SecondaryIndex Schema = " + ind.AttributeName + " " + ind.KeyType.Value);
						}
					}
				}

				if (response != null)
					response(sb.ToString(), null);

				PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString());
			});
		}

		/// <summary>
		/// Basic scan table method, does not have any advanced functionality.
		/// Be wary, scans are intensive
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="response"></param>
		/// <param name="go"></param>
		/// <param name="nextMethod"></param>
		public void ScanTable(string tableName, DDBScanResponseDelegate response = null, GameObject go = null, string nextMethod = null)
		{
			ScanTable(tableName, CapacityType.NONE, response, go, nextMethod);
		}

		/// <summary>
		/// Basic scan table method, does not have any advanced functionality.
		/// Be wary, scans are intensive
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="response"></param>
		/// <param name="go"></param>
		/// <param name="nextMethod"></param>
		public void ScanTable(string tableName, CapacityType returnedCapacity, DDBScanResponseDelegate response = null, GameObject go = null, string nextMethod = null)
		{
			try
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("ScanTable starting for Table: " + tableName);

				ScanRequest request = new ScanRequest();
				request.TableName = tableName;

				if (DBUnityHelper.SHOW_DEBUG)
					request.ReturnConsumedCapacity = new ReturnConsumedCapacity(returnedCapacity.ToString());

				DBConnect.Client.ScanAsync(request, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("ScanTable", result.Exception);
						if (response != null)
							response(null, go, nextMethod, result.Exception);
					}
					else
					{
						if (response != null)
							response(result.Response.Items, go, nextMethod, null);
						if (DBUnityHelper.SHOW_DEBUG)
						{
							Debug.Log("ScanTable DynamoDB response len=" + result.Response.Items.Count);
							foreach (Dictionary<string, AttributeValue> item in result.Response.Items)
							{
								Debug.Log("-----------------------------");
								DBTools.PrintDictionaryValues(item);
							}
						}
					}

					PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString(), result.Response.ConsumedCapacity);
				});
			}
			catch (Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("Exception - ScanTable: " + e.Message);
				if (response != null)
					response(null, go, nextMethod, e);
			}
		}

		/// <summary>
		/// More advanced scan table
		/// 
		/// Project Expression Info: 
		/// http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.AccessingItemAttributes.html#Expressions.AccessingItemAttributes.ProjectionExpressions
		/// 
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="indexName"></param>
		/// <param name="itemLimit"></param>
		/// <param name="exclusiveStartKey"></param>
		/// <param name="response"></param>
		/// <param name="go"></param>
		/// <param name="nextMethod"></param>
		public void ScanTableAdvanced(
			string tableName, 
			string indexName,
			int itemLimit,
			string projectionExpression, // filter for what to request
			string filterExpression, // filter for what to return
			CapacityType returnedCapacity,
			Dictionary<string, AttributeValue> exclusiveStartKey,
			DDBScanResponseDelegate response = null, 
			GameObject go = null, 
			string nextMethod = null)
		{
			try
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("ScanTableAdvanced starting for Table: " + tableName);

				ScanRequest request = new ScanRequest();
				request.TableName = tableName;
				if (!string.IsNullOrEmpty(indexName))
					request.IndexName = indexName;
				if (itemLimit > 0)
					request.Limit = itemLimit;
				if (exclusiveStartKey != null)
					request.ExclusiveStartKey = exclusiveStartKey;
				if (!string.IsNullOrEmpty(projectionExpression))
					request.ProjectionExpression = projectionExpression;
				if (!string.IsNullOrEmpty(filterExpression))
					request.FilterExpression = filterExpression;

				if (DBUnityHelper.SHOW_DEBUG)
					request.ReturnConsumedCapacity = new ReturnConsumedCapacity(returnedCapacity.ToString());

				DBConnect.Client.ScanAsync(request, (result) =>
				{
					if (result.Exception != null)
					{
						if (DBUnityHelper.SHOW_DEBUG)
							DBTools.PrintException("ScanTableAdvanced", result.Exception);
						if (response != null)
							response(null, go, nextMethod, result.Exception);
					}
					else
					{
						if (response != null)
							response(result.Response.Items, go, nextMethod, null);

						if (DBUnityHelper.SHOW_DEBUG)
						{
							Debug.Log("ScanTableAdvanced DynamoDB response len=" + result.Response.Items.Count);
							foreach (Dictionary<string, AttributeValue> item in result.Response.Items)
							{
								Debug.Log("-----------------------------");
								DBTools.PrintDictionaryValues(item);
							}
						}
					}

					PrintDebugInfo(result.Response.ContentLength, result.Response.HttpStatusCode.ToString(), result.Response.ConsumedCapacity);
				});
			}
			catch (Exception e)
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("Exception - ScanTableAdvanced: " + e.Message);
				if (response != null)
					response(null, go, nextMethod, e);
			}
		}

		/// <summary>
		/// Updates provisioned throughput for read/write capacity
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="readCapacity"></param>
		/// <param name="writeCapacity"></param>
		public void UpdateTableProvisionedThroughput(string tableName, int readCapacity, int writeCapacity)
		{
			UpdateTableRequest request = new UpdateTableRequest()
			{
				TableName = tableName,
				ProvisionedThroughput = new ProvisionedThroughput()
				{
					// Provide new values.
					ReadCapacityUnits = readCapacity,
					WriteCapacityUnits = writeCapacity
				}
			};

			DBConnect.Client.UpdateTableAsync(request, (result) =>
			{
				if (result.Exception != null)
				{
					Debug.LogError(result.Exception.Message);
				}
				else
				{
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DeleteTable Complete, response status code = " + result.Response.HttpStatusCode);
				}
			});
		}

		/// <summary>
		/// Centralized print for consumed capacity
		/// </summary>
		/// <param name="response"></param>
		public void PrintDebugInfo(long contentLen, string httpCode, ConsumedCapacity cap = null)
		{
			if (DBUnityHelper.SHOW_DEBUG)
			{
				sb.Length = 0;
				sb.Append("Content Length=").Append(contentLen).Append(" HTTP Response=").Append(httpCode);
				Debug.Log(sb.ToString());

				if (cap != null)
				{
					sb.Length = 0;
					sb.Append("** Consumed Capacity Units for [").Append(cap.TableName).Append("]: ");
					sb.Append(" Total=").Append(cap.CapacityUnits);
					sb.Append(" Table=").Append(cap.Table);
					sb.Append(" LocalIndex=").Append(cap.LocalSecondaryIndexes);
					sb.Append(" GlobalIndex=").Append(cap.GlobalSecondaryIndexes);
					Debug.Log(sb.ToString());
				}
			}
		}
	}
}
