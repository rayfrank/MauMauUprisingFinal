//----------------------------------------------
// DynamoDB Helper
// Copyright © 2014-2015 OuijaPaw Games LLC
//----------------------------------------------

using UnityEngine;
using System;
using System.Net;
using System.Net.Security;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.CognitoIdentity;
using Amazon.Runtime;

namespace DDBHelper
{
	/// <summary>
	/// DBConnect creates the connection and context object for DynamoDB
	/// 
	/// A word of caution.  Mono (the framework) does not use the computer/user's certificate store.  Its own certificate store is
	/// completely empty, so all certificates are rejected.  Nice.  As such, we are using a generic certificate validation callback 
	/// to approve the certificate from Amazon.  Sometimes this would fail when using DDBHelper over a VPN and proxy.
	/// 
	/// A solution to this was to use the mozroots.exe command line tool to import the computer's certificates.
	/// 
	/// From http://mono-project.com/FAQ:_Security:
	/// There are three alternatives to solve this problem:
	/// 1. Implement a ICertificatePolicy class. By doing this you can override the normal results of the certificate validation 
	///    (e.g. accepting an untrusted certificate). However you are now responsible of applying your own trust rules for your application. 
	///    Further suggestions and source code are available in the UsingTrustedRootsRespectfully article.
	/// 2. Use the certmgr.exe tool (included in Mono) to add the root certificates into the Mono Trust store. Every SSL certificate 
	///    signed from this root will then be accepted (i.e. no exception will be thrown) for SSL usage (for all Mono applications running 
	///    for the user or the computer - depending on the certificate store where the certificate was installed).
	/// 3. Use the mozroots.exe tool (included in Mono 1.1.10 and later) to download and install all Mozilla's root certificates 
	///    (i.e. the ones used in FireFox and other Mozilla's softwares). It's easier than finding a specific root but it's also less 
	///    granular to make a decision about which one(s) you install or not.
	///    
	/// I used option 3 and it works fine.  Just search your Unity install folder for mozroots.exe
	/// </summary>
	public class DBConnect
	{
		public IAmazonDynamoDB Client { get; private set; }
		public DynamoDBContext Context { get; private set; }

		private AWSCredentials _credentials;

		/// <summary>
		/// Region Endpoint information can be found here:
		/// http://docs.aws.amazon.com/general/latest/gr/rande.html#ddb_region
		/// </summary>
		public enum region : int
		{
			US_East_1 = 0,
			US_East_2 = 1,
			US_West_1 = 2,
			US_West_2 = 3,
            EU_West_1 = 4,
            EU_West_2 = 13,
            EU_Central_1 = 5,
			AP_South_1 = 6,
			AP_Southeast_1 = 7,
			AP_Southeast_2 = 8,
            AP_Northeast_1 = 9,
            AP_Northeast_2 = 11,
            SA_East_1 = 10,
            CA_Central_1 = 12,
		}

		/// <summary>
		/// basic constructor
		/// </summary>
		public DBConnect()
		{
			Client = null;
			Context = null;
		}

		/// <summary>
		/// Creates basic AWS Creadentials for login
		/// Read the readme to be sure this is what you want to do...
		/// </summary>
		/// <param name="accessID"></param>
		/// <param name="secretID"></param>
		public void InitRemoteConnection(string accessID, string secretID)
		{
			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("DBConnect - Creating Basic AWS Credentials");
			_credentials = new BasicAWSCredentials(accessID, secretID);
			CompleteRemoteConnection();
		}

		/// <summary>
		/// Creates credentials for Cognito Login
		/// Instructions on how to get this value are in the readme
		/// </summary>
		/// <param name="poolId"></param>
		public void InitRemoteConnection(string poolId)
		{
			// creating very basic cognito credentials
			if (DBUnityHelper.SHOW_DEBUG)
				Debug.Log("DBConnect - Creating Cognito Credentials");
			_credentials = new CognitoAWSCredentials(poolId, GetRegionEndpoint());
			CompleteRemoteConnection();
		}

		/// <summary>
		/// Does the meaty part of the connection
		/// This will connect to the live DynamoDB endpoint on some AWS server.
		/// </summary>
		public void CompleteRemoteConnection()
		{
			try
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBConnect - Initiating Connection...");

				// callback used to validate the certificate in an SSL conversation
				ServicePointManager.ServerCertificateValidationCallback = (RemoteCertificateValidationCallback)((s, ce, ca, p) => true);

				// configuration explains where/what we're doing with AWS
				AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();
				clientConfig.RegionEndpoint = GetRegionEndpoint(true);

				// note: if your proxy password is incorrect, it is --very-- hard to find an error, nothing throws an exception
				// except very rare cases.  Nothing works (save/load/create/etc.) and usually no errors thrown.  I found out the hard way.
				if (DBUnityHelper.Instance.useProxyForConnection)
				{
					if (DBUnityHelper.SHOW_DEBUG)
						Debug.Log("DBConnect - Setting up proxy configurations");
					clientConfig.ProxyHost = DBUnityHelper.Instance.proxyHostName;
					clientConfig.ProxyPort = DBUnityHelper.Instance.proxyPortNumber;
					clientConfig.ProxyCredentials = DBUnityHelper.Instance.proxyCredentials;
				}

				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBConnect - Setting up DynamoDB client");
				Client = new AmazonDynamoDBClient(_credentials, clientConfig);

				Debug.Log("DBConnect - Creating the context object");
				Context = new DynamoDBContext(Client);
			}
			catch (AmazonDynamoDBException e)
			{
				// Example of very detailed exception information
				Debug.Log("AmazonDynamoDBException - Message:" + e.Message);
				Debug.Log("--- HTTP Status Code: " + e.StatusCode);
				Debug.Log("--- AWS Error Code:   " + e.ErrorCode);
				Debug.Log("--- Error Type:       " + e.ErrorType);
				Debug.Log("--- Request ID:       " + e.RequestId);
			}
			catch (WebException e)
			{
				if (e.Status != WebExceptionStatus.TrustFailure)
				{
					// retry if possible
					Debug.Log("DBConnect - WebExceptionStatus.TrustFailure");
					throw;
				}
				Debug.Log("DBConnect - WebException : msg=" + e.Message);
			}
			catch (Exception e)
			{
				Debug.Log("DBConnect - Exception : msg=" + e.Message);
			}
		}

		/// <summary>
		/// Uses a DynamoDB Local Instance for testing
		/// DynamoDB Local attempts to emulate the actual DynamoDB service as closely as possible; however, there are several differences:
		/// 
		/// From Documentation, Things to be Aware Of:
		/// Regions and distinct AWS accounts are not supported at the client level.
		/// 
		/// DynamoDB Local ignores provisioned throughput settings, even though the API requires them. 
		/// For CreateTable, you can specify any numbers you want for provisioned read and write throughput, 
		/// even though these numbers will not be used. You can call UpdateTable as many times as you like per day; 
		/// however, any changes to provisioned throughput values are ignored.
		/// 
		/// DynamoDB Local does not throttle read or write activity. CreateTable, UpdateTable and DeleteTable operations occur 
		/// immediately, and table state is always ACTIVE. The speed of read and write operations on table data are limited only 
		/// by the speed of your computer.
		/// 
		/// Read operations in DynamoDB Local are eventually consistent. However, due to the speed of DynamoDB Local, most reads 
		/// will actually appear to be strongly consistent.
		/// 
		/// DynamoDB Local does not keep track of consumed capacity. In API responses, nulls are returned instead of capacity units.
		/// 
		/// DynamoDB Local does not keep track of item collection metrics; nor does it support item collection sizes. In API 
		/// responses, nulls are returned instead of item collection metrics.
		/// 
		/// In the DynamoDB API, there is a 1 MB limit on data returned per result set. The DynamoDB service enforces this limit, 
		/// and so does DynamoDB Local. However, when querying an index, DynamoDB only calculates the size of the projected key and 
		/// attributes. By contrast, DynamoDB Local calculates the size of the entire item.
		/// </summary>
		/// <param name="port"></param>
		public void InitLocalConnection(int port)
		{
			try
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBConnect - Initializing Connection");

				// callback used to validate the certificate in an SSL conversation
				//ServicePointManager.ServerCertificateValidationCallback = (RemoteCertificateValidationCallback)((s, ce, ca, p) => true);

				AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();

				clientConfig.ServiceURL = "http://localhost:" + port;

				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBConnect - Setting up DynamoDB client");
				Client = (IAmazonDynamoDB)new AmazonDynamoDBClient(DBUnityHelper.Instance.AccessKeyID, "NotRequired", clientConfig);

				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBConnect - Creating the context object");
				Context = new DynamoDBContext(Client);
			}
			catch (AmazonDynamoDBException e)
			{
				// Example of very detailed exception information
				Debug.Log("AmazonDynamoDBException - Message:" + e.Message);
				Debug.Log("--- HTTP Status Code: " + e.StatusCode);
				Debug.Log("--- AWS Error Code:   " + e.ErrorCode);
				Debug.Log("--- Error Type:       " + e.ErrorType);
				Debug.Log("--- Request ID:       " + e.RequestId);
			}
			catch (WebException e)
			{
				if (e.Status != WebExceptionStatus.TrustFailure)
				{
					// retry if possible
					Debug.Log("DBConnect - WebExceptionStatus.TrustFailure");
					throw;
				}
				Debug.Log("DBConnect - WebException : msg=" + e.Message);
			}
			catch (Exception e)
			{
				Debug.Log("DBConnect - Exception : msg=" + e.Message);
			}
		}

		/// <summary>
		/// Translates the defined enum from the DBUnityHelper into an actual RegionEndPoint that is
		/// usable for the connection.
		/// The regions below are the only ones listed in the AWS region listing as of May 6th, 2014
		/// http://docs.aws.amazon.com/general/latest/gr/rande.html#ddb_region
		/// I did not include the GovCloud endpoints.
		/// </summary>
		/// <returns></returns>
		public RegionEndpoint GetRegionEndpoint(bool showInfo = false)
		{
			RegionEndpoint endpoint = null;

			switch (DBUnityHelper.Instance.regionEndPoint)
			{
				case region.US_East_1:
					endpoint = RegionEndpoint.USEast1;
					break;
				case region.US_East_2:
					endpoint = RegionEndpoint.USEast2;
					break;
				case region.US_West_1:
					endpoint = RegionEndpoint.USWest1;
					break;
				case region.US_West_2:
					endpoint = RegionEndpoint.USWest2;
					break;
                case region.EU_West_1:
                    endpoint = RegionEndpoint.EUWest1;
                    break;
                case region.EU_West_2:
                    endpoint = RegionEndpoint.EUWest2;
                    break;
                case region.EU_Central_1:
					endpoint = RegionEndpoint.EUCentral1;
					break;
				case region.AP_South_1:
					endpoint = RegionEndpoint.APSouth1;
					break;
				case region.AP_Southeast_1:
					endpoint = RegionEndpoint.APSoutheast1;
					break;
				case region.AP_Southeast_2:
					endpoint = RegionEndpoint.APSoutheast2;
					break;
                case region.AP_Northeast_1:
                    endpoint = RegionEndpoint.APNortheast1;
                    break;
                case region.AP_Northeast_2:
                    endpoint = RegionEndpoint.APNortheast2;
                    break;
                case region.SA_East_1:
                    endpoint = RegionEndpoint.SAEast1;
                    break;
                case region.CA_Central_1:
                    endpoint = RegionEndpoint.CACentral1;
                    break;
                default:
					break;
			}
			
			if (DBUnityHelper.SHOW_DEBUG && showInfo)
				Debug.Log("DBConnect - RegionEndpoint is " + endpoint.DisplayName);
			
			return endpoint;
		}

		/// <summary>
		/// returns a response object which has the same table name as requested
		/// used primarily for testing purposes to see if you've connected to a table/database
		/// Could use this just to get a bunch of info as needed as well.
		/// </summary>
		public static void TestAWSConnection(string tableName)
		{
			try
			{
				if (DBUnityHelper.SHOW_DEBUG)
					Debug.Log("DBConnect - Testing AWS Connect by Describing Table: " + tableName);

				DescribeTableRequest request = new DescribeTableRequest
				{
					// remember, table name is case sensitive too
					TableName = tableName
				};

				DBUnityHelper.dbConnect.Client.DescribeTableAsync(request, (result) =>
				{
					if (result.Exception != null)
					{
						Debug.LogError(result.Exception.Message);
					}
					else
					{
						// this should match the table name you supplied above
						Debug.Log("CheckAWSConnection: Response Success - Table Name = " + result.Response.Table.TableName);
						Debug.Log("Table Item Count = " + result.Response.Table.ItemCount);
						Debug.Log("TableB Byte Size = " + result.Response.Table.TableSizeBytes);
					}
				});
			}
			catch (System.Exception e)
			{
				Debug.LogError("CheckAWSConnection:" + e.Message);
			}
		}
	}
}
