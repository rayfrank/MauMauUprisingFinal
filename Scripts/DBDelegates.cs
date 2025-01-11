//----------------------------------------------
// DynamoDB Helper
// Copyright © 2014-2015 OuijaPaw Games LLC
//----------------------------------------------

using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using UnityEngine;

namespace DDBHelper
{
    /// <summary>
    /// Just a nice placeholder for all delegate definitations
    /// </summary>
    public delegate void NextMethod();

    public delegate void DDBGenericDelegate(object o, Exception e = null);
    public delegate void DDBCompletionDelegate(bool success, Exception e = null);
    public delegate void DDBReadObjDelegate(DBObject response, Exception e = null);
    public delegate void DDBLoadObjDelegate<T>(T response, Exception e = null);
    public delegate void DDBExceptionDelegate(Exception e = null);
    public delegate void DDBAsyncCallback(IAsyncResult asyncResult);
    public delegate void DDBSuccessCallback(bool success);
    public delegate void DDBDescribeTableDelegate(string response, Exception e = null);
    public delegate void DDBQueryHashKeyOnlyDelegate(List<Dictionary<string, AttributeValue>> response, Exception e = null);
    public delegate void DDBQueryHashKeyOnlyDelegate<T>(List<T> response, Exception e = null);

    public delegate void DDBGenericResponseDelegate(object o, GameObject obj, string nextMethod, Exception e = null);
    public delegate void DDBCompletionResponseDelegate(bool success, GameObject obj, string nextMethod, Exception e = null);
    public delegate void DDBReadObjResponseDelegate(DBObject response, GameObject obj, string nextMethod, Exception e = null);
    public delegate void DDBLoadObjResponseDelegate<T>(T response, GameObject obj, string nextMethod, Exception e = null);
    public delegate void DDBScanResponseDelegate(List<Dictionary<string, AttributeValue>> response, GameObject obj, string nextMethod, Exception e = null);
    public delegate void DDBExceptionNextDelegate(GameObject obj, string nextMethod, Exception e = null);

    public delegate void DDBCompletionResponseDelegateMono(bool success, MonoBehaviour obj, string nextMethod, Exception e = null);
    public delegate void DDBLoadObjResponseDelegateMono<T>(T response, MonoBehaviour obj, string nextMethod, Exception e = null);
    
    ////////////////////////////////////////////////////////////////////
    // Everything Below Under Construction - Seeing about using an event handler/listener
    ///////////////////////////////////////////////////////////////////
    public delegate void DBEventHandler(object o, DBEventArgs e);

    public class DBEventArgs : EventArgs
    {
        public bool mSuccess { get; private set; }
        public Exception mException { get; private set; }

        public DBEventArgs(bool result, Exception e)
        {
            mSuccess = result;
            mException = e;
        }
    }

    public class DBListener
    {
        public void Listen(object o, DBEventArgs e)
        {
            Debug.Log("DBListener " + e.mSuccess);
        }
    }

    public class BusterBoy
    {
        public static event DBEventHandler DBEvent;

        public static void Main()
        {
            DBListener listener = new DBListener();
            DBEvent += new DBEventHandler(listener.Listen);
            Test();
        }

        public static void OnEvent(DBEventArgs e)
        {
            if (DBEvent != null)
                DBEvent(new object(), e);
        }

        public static void Test()
        {
            for (int i = 0; i < 99; i++)
            {
                if (i % 7 == 0)
                {
                    DBEventArgs e1 = new DBEventArgs(false, null);
                    OnEvent(e1);
                }
            }
        }

    }
}
