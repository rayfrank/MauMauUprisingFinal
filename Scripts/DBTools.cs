//----------------------------------------------
// DynamoDB Helper
// Copyright © 2014-2015 OuijaPaw Games LLC
//----------------------------------------------

using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using UnityEngine;

namespace DDBHelper
{
    public class DBTools
    {
        /// <summary>
        /// Enumeration for our simplicity to translate into the actual QueryOperator string later
        /// </summary>
        public enum QueryOperator : int
        {
            Equal,
            LessThanOrEqual,
            LessThan,
            GreaterThanOrEqual,
            GreaterThan,
            BeginsWith,
            Between,
        }

        /// <summary>
        /// Runs all exceptions through here.  Update as needed.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="ex"></param>
        public static void PrintException(string methodName, Exception ex)
        {
            if (ex is AmazonDynamoDBException)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                AmazonDynamoDBException ae = (AmazonDynamoDBException)ex;
                sb.AppendLine("AmazonDynamoDBException:" + ae.Message);
                sb.AppendLine("--- HTTP Status Code:   " + ae.StatusCode);
                sb.AppendLine("--- AWS Error Code:     " + ae.ErrorCode);
                sb.AppendLine("--- Error Type:         " + ae.ErrorType);
                sb.AppendLine("--- Request ID:         " + ae.RequestId);
                sb.AppendLine("--- StackTrace:         " + ae.StackTrace);
                Debug.LogError(sb.ToString());
            }
            else
            {
                Debug.LogError(methodName + ": " + ex.ToString());
            }
        }

        /// <summary>
        /// Used for testing/debugging to see each type of value and the data of that value
        /// in an AttibuteValue pair
        /// </summary>
        /// <param name="dict"></param>
        public static void PrintDictionaryValues(Dictionary<string, AttributeValue> dict)
        {
            int counter = 0;
            foreach (var keyValuePair in dict)
            {
                Debug.Log("KeyValuePair #" + counter++ +
                          " K=" + keyValuePair.Key +
                          " S=" + keyValuePair.Value.S +
                          " N=" + keyValuePair.Value.N +
                          " SS=" + string.Join(", ", keyValuePair.Value.SS.ToArray() ?? new List<string>().ToArray()) +
                          " NS=" + string.Join(", ", keyValuePair.Value.NS.ToArray() ?? new List<string>().ToArray()));
            }
        }

        /// <summary>
        /// Converts the enum selection for QueryType into the usable, corresponding string for DynamoDB Query
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ConvertQueryOperator(QueryOperator type)
        {
            string str;
            switch (type)
            {
                default:
                case QueryOperator.Equal:
                    str = "EQ";
                    break;
                case QueryOperator.LessThanOrEqual:
                    str = "LE";
                    break;
                case QueryOperator.LessThan:
                    str = "LT";
                    break;
                case QueryOperator.GreaterThanOrEqual:
                    str = "GE";
                    break;
                case QueryOperator.GreaterThan:
                    str = "GT";
                    break;
                case QueryOperator.BeginsWith:
                    str = "BEGINS_WITH";
                    break;
                case QueryOperator.Between:
                    str = "BETWEEN";
                    break;
            }
            return str;
        }
    }
}
