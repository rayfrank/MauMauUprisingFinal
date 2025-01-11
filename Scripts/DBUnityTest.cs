//----------------------------------------------
// DynamoDB Helper
// Copyright � 2014-2015 OuijaPaw Games LLC
//----------------------------------------------

using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using UnityEngine;

namespace DDBHelper
{
	/// <summary>
	/// Class used on a GameObject to test connections and some examples
	/// </summary>
	public class DBUnityTest : MonoBehaviour
	{
#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3
		[Header("------  Uses Test Table Name  ------")]
#endif
		public string testAWSTableName = "DDBHelper";
		public bool testAWSConnection = false;
		public bool testCreateTable = false;
		public bool testDeleteTable = false;
		public bool testListTables = false;
		public bool testDescribeTable = false;
		public bool testScanTable = false;
		public bool testExists = false;

#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3
		[Header("------  Requires Table Name and Hash Key  ------")]
#endif
		public bool testHashKeyOnly = false;
		public string testHashKeyOnlyTable = "TableName";
		public string testHashKeyOnlyKeyNamehashKey = "Hash Key Name";

#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3
		[Header("------  Requires Table Name Edit in File  ------")]
#endif
		public bool testSaveAndLoad = false;
		public bool testAsyncSave = false;
		public bool testAsyncLoad = false;

#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3
		[Header("------  Uses DBHashKeyContextQuery File  ------")]
#endif
		public bool testQueryHashKeyItem = false;

#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3
		[Header("------  Uses DBExample File  ------")]
#endif
		public bool testDBExample = false;

#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3
		[Header("------  Uses DBExamplePartitionKeyOnly File  ------")]
#endif
		public bool testPartitionKeyOnlyUpdate = false;

		// Use this for initialization
		void Start()
		{
			// Simple test to connect to database and print out some table info
			if (testAWSConnection)
				DBConnect.TestAWSConnection(testAWSTableName);

			if (testScanTable)
			{
				DBWorker.Instance.ScanTable(testAWSTableName, CapacityType.INDEXES);
			}

			if (testExists)
			{
				DBWorker.Instance.Exists(testAWSTableName, "Name", "alex", "Type", "acc",
					delegate(bool success, Exception e)
					{
						if (e != null)
							Debug.Log("Exists msg=" + e.Message);
						Debug.Log("Exists Result was " + success);
					});
			}

			// you will need to provide a hashkey for this to work
			// it will return a list of all attributes for each hash key present
			if (testHashKeyOnly)
			{                
				DBWorker.Instance.QueryHashKeyOnly(testHashKeyOnlyTable, // table name is case-sensitive
					testHashKeyOnlyKeyNamehashKey.ToLower(), // note I always set the hashkey to lowercase.  It is case-sensitive!
					new System.Collections.Generic.List<string> { }, // here you'd filter what attributes to get, or leave it blank for all attributes
					delegate (List<Dictionary<string, AttributeValue>> list, Exception e) // return delegate, have it inline or define a method to call
					{
						Debug.Log("-- Query Hash Key Completed, Printing Response Values --");
						foreach (var dict in list)
							DBTools.PrintDictionaryValues(dict);
					},
					10); // this last number is IMPORTANT as it limits how many items are returned.  you can consume capacity QUICKLY if you aren't aware
			}

			if (testDescribeTable)
			{
				DBWorker.Instance.DescribeTable(testAWSTableName,
					delegate (string response, Exception e)
					{
						Debug.Log("Describe Table Response: " + response);
					});
			}

			if (testDeleteTable)
			{
				DBWorker.Instance.DeleteTable(testAWSTableName,
					delegate(bool success, Exception e)
					{
						if (e != null)
							Debug.Log("Delete Table Exception msg=" + e.Message);
						else
							Debug.Log("Delete Table Response was " + success);
					});
			}

			if (testCreateTable)
			{
				// This is for creating secondary indexes when creating the table
				// No attributes should be specified to be projected unless projection type is INCLUDE
				// so that 'new List<string>()' should be empty unless the ProjectionType is INCLUDE
				// uncomment to use, include in the CreateTable where the nulls are
				//LocalSecondaryIndex index = DBWorker.Instance.CreateLocalSecondaryIndex("Name", "EM-index", "EM",
				//    new List<string>(), Amazon.DynamoDBv2.ProjectionType.ALL);

				// we need to define what kind of key the secondary index is using, S=string N=number B=binary
				//List<AttribType> attribTypes = new List<AttribType>() { AttribType.S };

				//List<LocalSecondaryIndex> indexes = new List<LocalSecondaryIndex>() { index };

				//if (indexes.Count != attribTypes.Count)
				//{
				//    Debug.LogError("Number of AttributeTypes does not match Number of Indexes");
				//}
				//else
				{
					DBWorker.Instance.CreateTable(testAWSTableName,
						"Name", DDBHelper.AttribType.S,
						"Type", DDBHelper.AttribType.S, 1, 1,
						null,
						null,
						delegate(bool success, Exception e)
						{
							if (e != null)
								Debug.Log("Create Table Exception msg=" + e.Message);
							else
								Debug.Log("Create Table Response was " + success);
						});
				}
			}

			if (testListTables)
			{
				Debug.Log(DBWorker.Instance.ListTables());
			}
			
			// from Example2.cs, there is PlayerData and PlayerAccount simulating save/loading account info
			if (testSaveAndLoad)
				StartCoroutine(PlayerAccount.TestDatabase());

			// Uses DBHashKeyContextQuery.cs file, and will fill a list of type T items, which in this case is
			// DBHashKeyContextQuery instances
			if (testQueryHashKeyItem)
				DBHashKeyContextQuery.TestHashKeyQueryForItem();

			// Uses DBExample.cs static method, and will handle creating/destroying/testing/etc.
			// Look at the DBExample class for more information on what is going on
			if (testDBExample)
				DBExample.TestSaveAndLoad();

			if (testPartitionKeyOnlyUpdate)
			{
				DBExamplePartitionKeyOnly.TestUpdate();
			}

		}
	}
}
