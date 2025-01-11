//----------------------------------------------
// DynamoDB Helper
// Copyright © 2014-2015 OuijaPaw Games LLC
//----------------------------------------------

using UnityEngine;
using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace DDBHelper
{
    /// <summary>
    /// DataConverters are used to convert complex data types into more simple, readable/usable ones
    /// DynamoDB doesn't know what a custom class you define is, so you this to convert them into
    /// something more usable.
    /// 
    /// You can serialize them, or convert them into strings, XML, whatever.
    /// I generally convert it into a string as it is smallest to manage, and can edit/view from the AWS Console
    /// </summary>
    public class DBDataConverter
    {
        /// <summary>
        /// Converts the ComplexType to string and vice-versa
        /// You still have to use the defined types of string, number, or binary and handle it in some fashion
        /// It is much more limited than the low-level API calls, unless you're a code genius
        /// </summary>
        public class ComplexDataConverter : IPropertyConverter
        {
            /// <summary>
            /// Converts complex data type into a string for DynamoDB
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public DynamoDBEntry ToEntry(object value)
            {
                DynamoDBEntry entry = null;
                try
                {
                    ComplexType type = value as ComplexType;
                    if (type == null)
                        throw new ArgumentOutOfRangeException();

                    // saving 3 variables with string delimiter
                    string data = string.Format("{1}{0}{2}{0}{3}", "|", type.name, type.x, type.y);

                    entry = new Primitive { Value = data };
                }
                catch (Exception e)
                {
                    DBTools.PrintException("ComplexDataConverter", e);
                }
                return entry;
            }

            /// <summary>
            /// Converts string from DynamoDB and expands it into the complex data type
            /// </summary>
            /// <param name="entry"></param>
            /// <returns></returns>
            public object FromEntry(DynamoDBEntry entry)
            {
                const int DATA_LENGTH = 3;
                ComplexType item = null;
                Primitive primitive = entry as Primitive;

                try
                {
                    if (primitive != null && primitive.Value is string)
                    {
                        string tempString = (string)primitive.Value;
                        if (string.IsNullOrEmpty(tempString))
                            throw new ArgumentOutOfRangeException();

                        // You know from how many are written above as to how many variables using string delimiter
                        string[] data = (tempString).Split(new string[] { "|" }, StringSplitOptions.None);
                        if (data.Length != DATA_LENGTH)
                            throw new ArgumentOutOfRangeException();

                        item = new ComplexType
                        {
                            name = Convert.ToString(data[0]),
                            x = Convert.ToByte(data[1]),
                            y = Convert.ToByte(data[2])
                        };
                    }
                }
                catch (Exception e)
                {
                    DBTools.PrintException("ComplexDataConverter", e);
                }
                return item;
            }
        }

        /// <summary>
        /// This is a more complex conversion example, which will convert a list or well... anything I surmise
        /// into a byte-array for binary usage.  The drawback I've found is that for saving lists and whatnot, it
        /// is rather inefficient... it makes things very easy, but you pay for it in other ways, such as not
        /// being able to edit/view the data in the DDB table
        /// </summary>
        public class ListToBinary : IPropertyConverter
        {
            /// <summary>
            /// Converts complex data type into a compressed memory stream for DynamoDB
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public DynamoDBEntry ToEntry(object value)
            {
                DynamoDBEntry entry = null;

                try
                {
                    List<ComplexType> type = value as List<ComplexType>;
                    if (type == null)
                        throw new ArgumentOutOfRangeException();

                    entry = new Primitive(DBObject.ToGzipMemoryStream(type));
                }
                catch (Exception e)
                {
                    DBTools.PrintException("ListToBinary", e);
                }
                return entry;
            }

            /// <summary>
            /// Converts gzip bitstream from database and converts it to a list of complex data types
            /// </summary>
            /// <param name="entry"></param>
            /// <returns></returns>
            public object FromEntry(DynamoDBEntry entry)
            {
                List<ComplexType> items = null;

                try
                {
                    Primitive primitive = entry as Primitive;
                    if (primitive == null || !(primitive.Value is byte[]))
                        throw new ArgumentOutOfRangeException();

                    items = DBObject.FromGzipMemoryStream<List<ComplexType>>(primitive.AsMemoryStream());
                }
                catch (Exception e)
                {
                    DBTools.PrintException("ListToBinary", e);
                }
                return items;
            }
        }
    }
    /// <summary>
    /// Converts the complex type to string and vice-versa
    /// You still have to use the defined types of string, number, or binary and handle it in some fashion
    /// It is much more limited than the low-level API calls, unless you're a code genius
    /// </summary>
    public class ListToString : IPropertyConverter
    {
        public static char[] ItemDelimiter = new char[] { '&' };
        public static char[] DataDelimiter = new char[] { '|' };
        public string[] mItems;
        public string[] mData;

        /// <summary>
        /// Converts complex data type into a string for DynamoDB
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public DynamoDBEntry ToEntry(object value)
        {
            DynamoDBEntry entry = null;
            try
            {
                List<ComplexType> type = value as List<ComplexType>;
                if (type == null)
                    throw new ArgumentOutOfRangeException();

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                ComplexType cTemp;
                for (int i = 0; i < type.Count; i++)
                {
                    cTemp = type[i];
                    sb.Append(cTemp.name).Append("|").Append(cTemp.x).Append("|").Append(cTemp.y);
                    if (i < type.Count - 1)
                        sb.Append("&");
                }
                cTemp = null;
                entry = new Primitive(sb.ToString());
            }
            catch (Exception e)
            {
                DBTools.PrintException("ListToString", e);
            }
            return entry;
        }

        /// <summary>
        /// Converts string from DynamoDB and expands it into the complex data type
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public object FromEntry(DynamoDBEntry entry)
        {
            List<ComplexType> items = new List<ComplexType>();
            Primitive primitive = entry as Primitive;

            try
            {
                if (primitive != null && primitive.Value is string)
                {
                    string tempString = (string)primitive.Value;
                    if (string.IsNullOrEmpty(tempString))
                        throw new ArgumentOutOfRangeException();

                    // split the string into the separate items first
                    mItems = tempString.Split(ItemDelimiter, StringSplitOptions.None);

                    for (int i = 0; i < mItems.Length; i++)
                    {
                        // now split the items into the data to use
                        mData = mItems[i].Split(DataDelimiter, StringSplitOptions.None);
                        items.Add(new ComplexType
                        {
                            name = Convert.ToString(mData[0]),
                            x = Convert.ToByte(mData[1]),
                            y = Convert.ToByte(mData[2])
                        });
                    }
                }
            }
            catch (Exception e)
            {
                DBTools.PrintException("ListToString", e);
            }
            return items;
        }
    }

    /// <summary>
    /// Converts the complex type DimensionType to string and vice-versa
    /// You still have to use the defined types of string, number, or binary and handle it in some fashion
    /// It is much more limited than the low-level API calls, unless you're a code genius
    /// </summary>
    public class DateTimeConverter : IPropertyConverter
    {
        public string[] mSlots;
        public string[] mData;
        private string tempString;

        /// <summary>
        /// Converts complex data type into a string for DynamoDB
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public DynamoDBEntry ToEntry(object value)
        {
            DynamoDBEntry entry = null;
            try
            {
                DateTime type = DateTime.UtcNow;
                if (value == null)
                    throw new ArgumentOutOfRangeException();
                else
                    type = (DateTime)value;
                entry = new Primitive(type.ToUniversalTime().ToString());
            }
            catch (Exception e)
            {
                DBTools.PrintException("DateTimeConverter", e);
            }
            return entry;
        }

        /// <summary>
        /// Converts string from DynamoDB and expands it into the complex data type
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public object FromEntry(DynamoDBEntry entry)
        {
            DateTime datetime = DateTime.UtcNow;

            try
            {
                Primitive primitive = entry as Primitive;
                if (primitive == null || !(primitive.Value is String))
                    throw new ArgumentOutOfRangeException();

                tempString = (string)primitive.Value;
                if (string.IsNullOrEmpty(tempString))
                    throw new ArgumentOutOfRangeException();

                //Debug.Log("FromEntry - string=" + tempString);
                if (!DateTime.TryParse(tempString, out datetime))
                    Debug.LogError("DateTimeConverter - Parse Fail: " + tempString);
            }
            catch (Exception e)
            {
                DBTools.PrintException("DateTimeConverter", e);
            }
            return datetime;
        }
    }
}
