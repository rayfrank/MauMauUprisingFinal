//----------------------------------------------
// DynamoDB Helper
// Copyright © 2014-2015 OuijaPaw Games LLC
//----------------------------------------------

using System.Collections.Generic;
using Amazon;
using UnityEngine;

namespace DDBHelper
{
	/// <summary>
	/// The DBUnityHelper has two functions:
	/// 
	/// 1.  It is a bunch fo queues that fill up, and each frame are drained
	/// This makes anything done outside of Unity's mainthread still threadsafe and able to interact.
	/// If you try to have the DBWorker return any information/messages/etc. to the main Unity thread,
	/// Unity will slap you in the face with a giant salmon.
	/// 
	/// 2.  It handles the setup of the connection/context object, etc. using a Unity object so you
	/// can change/edit the values in the inspector.  This is the only MonoBehavior object of the package.
	/// 
	/// Create a gameobject in your scene, attach this script object to it, edit the values, and it should
	/// be ready to go.
	/// </summary>
	public class DBUnityHelper : MonoBehaviour
	{
		public enum ConnectType : int
		{
			DDB_Cognito,
			DDB_Normal,
			DDB_Local
		}

		public static DBUnityHelper Instance;
		public static bool SHOW_DEBUG = false;

		private static Queue<SendMessageContext> _queuedMessages = new Queue<SendMessageContext>();
		private static Queue<SendCoroutineContext> _queuedCoroutines = new Queue<SendCoroutineContext>();

		public static DBConnect dbConnect { get; private set; }
#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3
		[Header("------  General  ------")]
#endif
		public bool showDebugMessages = false;
		//public bool automaticProvisionUpdates = false;
		//public int minProvisionedThroughput = 10;

#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3
		[Header("------  Connection  ------")]
#endif
		public ConnectType connectType;
		public DBConnect.region regionEndPoint;

		/// <summary>
		/// Cognito ID Pool is only available in select regions
		/// </summary>
		public string IdentityPoolId = "Use for Cognito ID";

		/// <summary>
		/// IF YOU ARE USING THESE, IT IS VERY TRIVIAL FOR SOMEONE TO EXTRACT FROM A COMPILED PROGRAM
		/// It may be 'more difficult' in iOS due to IL2CPP, but I wouldn't say it's impossible.
		/// Extracting data from a compiled Unity program is not difficult at all.  Be forewarned.
		/// </summary>
		public string AccessKeyID = "Use for DDBLocal / remote DDB";
		public string SecretKeyID = "Use for non-cognito remote DDB";

#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3
		[Header("------  DynamoDB Local  ------")]
#endif
		public int localPort = 8000;

#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3
		[Header("------  Proxy  ------")]
#endif
		public bool useProxyForConnection = false;
		public string proxyHostName = "";
		public int proxyPortNumber = 0;
		public string proxyUserName = "";
		public string proxyPassword = "";

		[SerializeField]
		public RegionEndpoint reg;

		public System.Net.NetworkCredential proxyCredentials { get; private set; }

		/// <summary>
		/// Initializes everything on Awake.  Since Awake order is undetermined, do not try to load anything
		/// from the DBWorker until Start() method or later.
		/// </summary>
		void Awake()
		{
			// Keeping a single instance of this class as reference
			if (Instance)
				Destroy(Instance);
			Instance = this;

			SHOW_DEBUG = showDebugMessages;

			Amazon.UnityInitializer.AttachToGameObject(this.gameObject);

#if UNITY_2017_1_OR_NEWER
            // *** this is the potential workaround fix for SDK incompatibility 
            Amazon.AWSConfigs.HttpClient = Amazon.AWSConfigs.HttpClientOption.UnityWebRequest;
#endif

            if (useProxyForConnection)
				proxyCredentials = new System.Net.NetworkCredential(proxyUserName, proxyPassword);

			dbConnect = new DBConnect();

			switch (connectType)
			{
				default:
				case ConnectType.DDB_Cognito:
					dbConnect.InitRemoteConnection(IdentityPoolId);
					break;
				case ConnectType.DDB_Normal:
					dbConnect.InitRemoteConnection(AccessKeyID, SecretKeyID);
					break;
				case ConnectType.DDB_Local:
					dbConnect.InitLocalConnection(localPort);
					break;
			}

			// creates the worker thread/connection/context/etc.
			DBWorker.InitializeForUnity(dbConnect, useProxyForConnection, (connectType == ConnectType.DDB_Local));
		}

		/// <summary>
		/// Used for registering a gameobject that will use SendMessage to invoke a method
		/// </summary>
		/// <param name="context"></param>
		public static void Register(SendMessageContext context)
		{
			lock (_queuedMessages)
			{
				_queuedMessages.Enqueue(context);
			}
		}

		/// <summary>
		/// Used for registering a MonoBehaviour that will use StartCoroutine to invoke an IEnumerator
		/// </summary>
		/// <param name="context"></param>
		public static void Register(SendCoroutineContext context)
		{
			lock (_queuedCoroutines)
			{
				_queuedCoroutines.Enqueue(context);
			}
		}

		/// <summary>
		/// This is dependent upon framerate.  Move to fixedupdate if there are performance issues.
		/// If you need to update graphics or something of the sort, move to lateupdate.
		/// </summary>
		private void Update()
		{
			while (_queuedMessages.Count > 0)
			{
				SendMessageContext context = null;
				lock (_queuedMessages)
				{
					context = _queuedMessages.Dequeue();
				}

				// rare case could have been destroyed
				if (context.Target)
					context.Target.SendMessage(context.MethodName, context.Value, context.Options);
			}

			while (_queuedCoroutines.Count > 0)
			{
				SendCoroutineContext context = null;
				lock (_queuedCoroutines)
				{
					context = _queuedCoroutines.Dequeue();
				}

				// rare case could have been destroyed
				if (context.Target)
					context.Target.StartCoroutine(context.MethodName, context.Value);
			}
		}
	}

	/// <summary>
	/// Used for storing reference to GameObject and methodname used for SendMessage
	/// </summary>
	public class SendMessageContext
	{
		public GameObject Target;
		public string MethodName;
		public object Value;
		public SendMessageOptions Options = SendMessageOptions.RequireReceiver;

		public SendMessageContext(GameObject target, string methodName, object value, SendMessageOptions options)
		{
			this.Target = target;
			this.MethodName = methodName;
			this.Value = value;
			this.Options = options;
		}
	}

	/// <summary>
	/// Used for storing reference to MonoBehaviour and methodname used for Coroutine
	/// Changed the target to a monobehaviour so they are easily differentiated
	/// </summary>
	public class SendCoroutineContext
	{
		public MonoBehaviour Target;
		public string MethodName;
		public object Value;

		public SendCoroutineContext(MonoBehaviour target, string methodName, object value)
		{
			this.Target = target;
			this.MethodName = methodName;
			this.Value = value;
		}
	}
}
