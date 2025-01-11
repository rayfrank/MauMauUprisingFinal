//----------------------------------------------
// DynamoDB Helper
// Copyright © 2014-2015 OuijaPaw Games LLC
//----------------------------------------------

using UnityEngine;
using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System.Collections;

/*
 * This has most the same commenting / setup as Example2, but removed a bunch of stuff
 * so it's easier to understand without the clutter of other stuff.
 * 
 * It's just a template.  Attribute specific save/load is possible, using a DBObject.
 */ 
namespace DDBHelper
{
    ///// <summary>
    ///// This is an example of a 'character slot' for a player, recording specific information
    ///// for future use.
    ///// </summary>
    //public class ComplexTemplate
    //{
    //    public short ID;
    //    public DateTime created;
    //    public byte whatever;
 
    //    public ComplexTemplate() { }
    //    public ComplexTemplate(short _ID, DateTime _created, byte _whatever)
    //    {
    //        ID = _ID;
    //        created = _created;
    //        whatever = _whatever;
    //    }
    //}
    
    ///// <summary>
    ///// This is an example class that just holds references to the bucket names in DynamoDB,
    ///// and static methods to separate them from the member methods of the DDBAccountTemplate class
    ///// </summary>
    //public class DBDataTemplate
    //{
    //    public delegate void NextMethod();

    //    // you'll need to change the table name to whatever you create
    //    public const string TABLE_NAME = "UpdateMe";

    //    // if you use a different hash key name, this will need to be changed
    //    public const string KEY_HASH = "UpdateMe";

    //    // if you use a different range key name, this will need to be changed
    //    public const string KEY_RANGE = "UpdateMe";

    //    public const string KEY_RANGE_ACCOUNT = "acc";

    //    public const string KEY_ACC_EMAIL = "EM";
    //    public const string KEY_ACC_PASS = "Pass";
    //    public const string KEY_ACC_WEALTH = "Wlth";
    //    public const string KEY_ACC_COMPLEX = "Cplx";

    //    /// <summary>
    //    /// Static method for deleting a character.
    //    /// </summary>
    //    /// <param name="name"></param>
    //    /// <param name="charID"></param>
    //    /// <param name="nextMethod"></param>
    //    public static void DeleteChararacter(string name, short charID, NextMethod nextMethod = null)
    //    {
    //        name = name.ToLower();
    //        Debug.Log("DeleteCharacter name=" + name + " charID=" + charID);
    //        DBObject tObj = new DBObject(name, charID.ToString());
    //        DBWorker.Instance.DeleteItem(DBDataTemplate.TABLE_NAME,
    //            tObj,
    //            delegate(bool success, Exception e)
    //            {
    //                if (e != null)
    //                    DBTools.PrintException("DeleteCharacter", e);
    //                Debug.Log("DeleteCharacter result=" + success);

    //                if (nextMethod != null)
    //                    nextMethod();
    //            });
    //    }

    //    /// <summary>
    //    /// Static method for checking if the name exists in the DDB - checks only the name, very efficient
    //    /// </summary>
    //    /// <param name="owner"></param>
    //    /// <param name="name"></param>
    //    /// <param name="methodName"></param>
    //    public static void CheckExists(GameObject owner, string name, string methodName = null)
    //    {
    //        name = name.ToLower();
    //        DBWorker.Instance.Exists(TABLE_NAME,
    //            name, KEY_RANGE_ACCOUNT,
    //            delegate(bool response, Exception e)
    //            {
    //                if (e != null)
    //                    DBTools.PrintException("CheckExists", e);
    //                Debug.Log("CheckExists result=" + response);

    //                DBUnityHelper.Register(
    //                    new SendMessageContext(owner, methodName, response, SendMessageOptions.RequireReceiver));
    //            });
    //    }
    //}

    ///// <summary>
    ///// This is an example of a player account that would be saved/loaded into DynamoDB
    ///// This is the same code I use for my game.
    ///// </summary>
    //[DynamoDBTable(DBDataTemplate.TABLE_NAME)]  // <---------------- UPDATE ME AS NEEDED!!!!!!!!!!!!!!!!!
    //public class DDBAccountTemplate
    //{
    //    public delegate void NextMethod();

    //    // PrimaryKey (PK) mapping -- required!
    //    [DynamoDBHashKey(DBDataTemplate.KEY_HASH)]
    //    public string mName { get; set; }

    //    [DynamoDBRangeKey(DBDataTemplate.KEY_RANGE)]
    //    public string mType = DBDataTemplate.KEY_RANGE_ACCOUNT;

    //    [DynamoDBProperty(DBDataTemplate.KEY_ACC_EMAIL)]
    //    public string mEmail { get; set; }

    //    [DynamoDBProperty(DBDataTemplate.KEY_ACC_PASS)]
    //    public string mPassword { get; set; }

    //    [DynamoDBProperty(DBDataTemplate.KEY_ACC_WEALTH)]
    //    public uint mWealth { get; set; }

    //    // note the complex types need a converter...
    //    [DynamoDBProperty(DBDataTemplate.KEY_ACC_COMPLEX, typeof(ComplexTemplateConverter))]
    //    public ComplexTemplate mComplex { get; set; }

    //    [DynamoDBIgnore]
    //    GameObject mPlayer { get; set; }

    //    // While this is available, I don't particularly like it... especially since Unity is single-threaded anyhow
    //    //[DynamoDBVersion]
    //    //public int? version { get; set; }

    //    /// <summary>
    //    /// Ideally we do not want to use a GameObject here.
    //    /// Use your player monobehaviour or whatever is the 'player' that identifies them
    //    /// This way the data object (this) has a reference back to the owner if necessary
    //    /// On the flip side, it also means this (the data object) can be independent of the player
    //    /// if necessary (like they try to drop connection moment they die, etc.)
    //    /// </summary>
    //    /// <param name="p"></param>
    //    public void Init(GameObject p)
    //    {
    //        //Debug.Log("DBDataTemplate Init()");
    //        mPlayer = p;
    //        mName = "";
    //        mEmail = "";
    //        mPassword = "";
    //        mWealth = 0;
    //        mComplex = new ComplexTemplate(2345, System.DateTime.Now, 128);
    //    }

    //    /// <summary>
    //    /// Loads the player's account from DynamoDB
    //    /// The delegate method being passed as the parameter is the important part of handling this in an elegant way
    //    /// Once the data has been loaded, that method will be called, such as moving to the next step in a login process
    //    /// </summary>
    //    /// <param name="owner"></param>
    //    /// <param name="name"></param>
    //    /// <param name="methodName"></param>
    //    public void Load(GameObject owner, string name, string methodName)
    //    {
    //        this.mName = name.ToLower();
    //        try
    //        {
    //            DBWorker.Instance.Load(this,
    //                delegate(DDBAccountTemplate response, Exception e)
    //                {
    //                    if (e != null)
    //                        DBTools.PrintException("Load", e);

    //                    if (response == null)
    //                    {
    //                        DBUnityHelper.Register(
    //                            new SendMessageContext(owner, methodName, false, SendMessageOptions.RequireReceiver));
    //                    }
    //                    else
    //                    {
    //                        Debug.Log("DDBAccountTemplate " + name + " Loaded Successfully");
    //                        this.mName = name;
    //                        this.mEmail = response.mEmail;
    //                        this.mPassword = response.mPassword;
    //                        this.mWealth = response.mWealth;
    //                        this.mComplex = response.mComplex;
    //                        DBUnityHelper.Register(
    //                            new SendMessageContext(owner, methodName, true, SendMessageOptions.RequireReceiver));
    //                    }
    //                });
    //        }
    //        catch (System.Exception e)
    //        {
    //            Debug.LogError("DDBAccountTemplate - Load msg=" + e.Message);
    //            DBUnityHelper.Register(
    //                new SendMessageContext(owner, methodName, false, SendMessageOptions.RequireReceiver));
    //        }
    //    }

    //    /// <summary>
    //    /// Saves account to DynamoDB
    //    /// </summary>
    //    /// <param name="owner"></param>
    //    /// <param name="name"></param>
    //    /// <param name="methodName"></param>
    //    public void Save(GameObject owner, string name, string methodName = null)
    //    {
    //        this.mName = name.ToLower();
    //        try
    //        {
    //            DBWorker.Instance.Save(this,
    //                delegate
    //                {
    //                    Debug.Log("DDBAccountTemplate " + name + " Saved Successfully");
    //                    if (methodName != null)
    //                        DBUnityHelper.Register(
    //                            new SendMessageContext(owner, methodName, true, SendMessageOptions.RequireReceiver));
    //                });

    //        }
    //        catch (System.Exception e)
    //        {
    //            Debug.LogError("DDBAccountTemplate " + name + " - Save Exception msg=" + e.Message);
    //            if (methodName != null)
    //                DBUnityHelper.Register(
    //                    new SendMessageContext(owner, methodName, false, SendMessageOptions.RequireReceiver));
    //        }
    //    }

    //    /// <summary>
    //    /// Saves character slots and wealth, used after buying a new character slot
    //    /// </summary>
    //    /// <param name="nextMethod"></param>
    //    public void SaveWealth(NextMethod nextMethod = null)
    //    {
    //        DBObject tObj = new DBObject(mName, DBDataTemplate.KEY_RANGE_ACCOUNT);
    //        tObj.PrepareUpdateNumber(DBDataTemplate.KEY_ACC_WEALTH, mWealth, true);

    //        DBWorker.Instance.UpdateItem(DBDataTemplate.TABLE_NAME,
    //            tObj,
    //            delegate(bool success, Exception e)
    //            {
    //                if (e != null)
    //                    DBTools.PrintException("SaveWealth", e);
    //                Debug.Log("SaveWealth result=" + success);
    //                if (nextMethod != null)
    //                    nextMethod();
    //            });
    //    }

    //    /// <summary>
    //    /// This is another Load method, but it does not have the GameObject tied to it.
    //    /// Since this result is returned in a separate thread from Unity, you cannot actually
    //    /// check whether a GameObject is null (that I'm aware) and therefore, for the test below
    //    /// which is not done from a monobehavior/unity, this Load method had to be created without
    //    /// the GameObject references.  Since there is no GameObject, there is no delegate method to
    //    /// call once it completes either.
    //    /// </summary>
    //    /// <param name="name"></param>
    //    public void Load(string name)
    //    {
    //        this.mName = name.ToLower();
    //        try
    //        {
    //            DBWorker.Instance.Load(this,
    //                delegate(DDBAccountTemplate response, Exception e)
    //                {
    //                    if (e != null)
    //                        DBTools.PrintException("Load", e);

    //                    if (response == null)
    //                    {
    //                        Debug.Log("DDBAccountTemplate " + name + " Load Failed");
    //                    }
    //                    else
    //                    {
    //                        Debug.Log("DDBAccountTemplate " + name + " Loaded Successfully");
    //                        this.mName = name;
    //                        this.mEmail = response.mEmail;
    //                        this.mPassword = response.mPassword;
    //                        this.mWealth = response.mWealth;
    //                    }
    //                });
    //        }
    //        catch (System.Exception e)
    //        {
    //            Debug.LogError("DDBAccountTemplate - Load msg=" + e.Message);
    //        }
    //    }
        
    //    /// <summary>
    //    /// This is a simple way of testing the database.
    //    /// Set to static method so it can be run most anywhere.
    //    /// This was pre-everything above.
    //    /// </summary>
    //    public static IEnumerator TestDatabase()
    //    {
    //        Debug.Log("DDBAccountTemplate - Initiating DynamoDB Test...");
    //        DDBAccountTemplate p = new DDBAccountTemplate();
    //        p.mName = "Yuki";
    //        p.mType = "Test";
    //        p.mEmail = "alexninja@gmail.com";
    //        p.mPassword = "passwordYo"; 
    //        p.mWealth = 2222;
    //        p.mComplex = new ComplexTemplate(23222, DateTime.Now, 128);

    //        // I have so many spots making the case lower.  Let me know if I should go about making booleans as to
    //        // whether you want case checks all over or not
    //        p.mName = p.mName.ToLower();

    //        Debug.Log("DDBAccountTemplate - Starting Save Test for " + p.mName + ":" + p.mType);

    //        bool hasSaved = false;

    //        // very kludgy check for whether something has saved or not
    //        // be aware something like this will actually stop the thread while waiting on a response
    //        // to the save.  A better way would be to invoke a delegate method for the return, or use
    //        // coroutines, etc.  This is just an example to show something saving, then loading.
    //        DBWorker.Instance.Save(p,
    //            delegate (bool success, Exception e)
    //            {
    //                if (e != null)
    //                    DBTools.PrintException("Load", e);

    //                Debug.Log("DDBAccountTemplate " + p.mName + " Save Result=" + success);
    //                hasSaved = true;
    //            });

    //        while (!hasSaved)
    //        {
    //            // yep do nothing... we're just waiting...  
    //            yield return new WaitForSeconds(.1f);
    //        }

    //        // could load the object directly in the code here...
    //        Debug.Log("DDBAccountTemplate - Starting Load Test for " + p.mName + ":" + p.mType);

    //        DBWorker.Instance.Load(p,
    //            delegate(DDBAccountTemplate response, Exception e)
    //            {
    //                if (e != null)
    //                    DBTools.PrintException("Load", e);

    //                if (response == null)
    //                {
    //                    Debug.Log("Response is NULL... Load Failure");
    //                }
    //                else
    //                {
    //                    p = response;
    //                    Debug.Log("Response mWealth=" + response.mWealth);
    //                    Debug.Log("Player mWealth=" + p.mWealth);
    //                    Debug.Log("Created complex type at " + p.mComplex.created);
    //                }
    //            });

    //        // or could load the object here using a class method
    //        p.Load(p.mName);

    //        yield return null;
    //    }
    //}



    ///// <summary>
    ///// Converts the complex type DimensionType to string and vice-versa
    ///// You still have to use the defined types of string, number, or binary and handle it in some fashion
    ///// It is much more limited than the low-level API calls, unless you're a code genius
    ///// </summary>
    //public class ComplexTemplateConverter : IPropertyConverter
    //{
    //    private const int DATA_LENGTH = 3;
    //    /// <summary>
    //    /// Converts complex data type into a string for DynamoDB
    //    /// </summary>
    //    /// <param name="value"></param>
    //    /// <returns></returns>
    //    public DynamoDBEntry ToEntry(object value)
    //    {
    //        DynamoDBEntry entry = null;
    //        try
    //        {
    //            ComplexTemplate type = value as ComplexTemplate;
    //            if (type == null)
    //                throw new ArgumentOutOfRangeException();

    //            // saving 5 variables with string delimiter
    //            string data = string.Format("{1}{0}{2}{0}{3}", "|", type.ID, type.created, type.whatever);

    //            entry = new Primitive { Value = data };
    //        }
    //        catch (Exception e)
    //        {
    //            Debug.Log("ComplexTemplateConverter " + e.Message);
    //        }
    //        return entry;
    //    }

    //    /// <summary>
    //    /// Converts string from DynamoDB and expands it into the complex data type
    //    /// </summary>
    //    /// <param name="entry"></param>
    //    /// <returns></returns>
    //    public object FromEntry(DynamoDBEntry entry)
    //    {
    //        ComplexTemplate cslot = null;
    //        try
    //        {
    //            Primitive primitive = entry as Primitive;
    //            if (primitive == null || !(primitive.Value is String) || string.IsNullOrEmpty((string)primitive.Value))
    //                throw new ArgumentOutOfRangeException();

    //            // reading 3 variables using string delimiter
    //            string[] data = ((string)(primitive.Value)).Split(new string[] { "|" }, StringSplitOptions.None);
    //            if (data.Length != DATA_LENGTH)
    //                throw new ArgumentOutOfRangeException();

    //            cslot = new ComplexTemplate
    //            {
    //                ID = Convert.ToInt16(data[0]),
    //                created = Convert.ToDateTime(data[1]),
    //                whatever = Convert.ToByte(data[2]),
    //            };
    //        }
    //        catch (Exception e)
    //        {
    //            Debug.Log("ComplexTemplateConverter " + e.Message);
    //        }
    //        return cslot;
    //    }
    //}
}