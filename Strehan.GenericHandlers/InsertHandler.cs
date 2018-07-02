/*
 Copyright (C) 2018 William Strehan
*/

using System;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Text;
using Strehan.DataAccess;

namespace Strehan.GenericHandlers
{
    /// <summary>
    /// Generic Insert Handler
    /// </summary>
    /// <remarks>
    /// Type TID is the type of the primary Key - just used for inserts so only used with type T
    /// <typeparam name="T">Type that defines the fields that are used for the insert</typeparam>
    /// <typeparam name="B">Type that defines the fields that are read back when getting a record by Id</typeparam>
    /// </remarks>
    public class InsertHandler<TID, T, B> : IHttpHandler
    {
        /// <summary>
        /// Connection String for SQL Server Database
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Stored procedure to be called with input parameters that match up with Type T
        /// </summary>
        public string InsertStoredProcedureName { get; set; }

        /// <summary>
        /// Stored procedure to be called with fields that match up with Type B
        /// </summary>
        public string GetByIdStoredProcedureName { get; set; }

        /// <summary>
        /// Called First before anything else is done in the handler
        /// </summary>
        public Func<HttpContext, PreExecutionResult> PreExecution { get; set; }

        /// <summary>
        /// Delegate that is called after database functions complete
        /// </summary>
        public Func<ReturnData, object> BusinessLogic { get; set; }

        /// <summary>
        /// Delegate that is called when there is an error
        /// </summary>
        public Func<ReturnData, Exception, object> ErrorHandling { get; set; }

        /// <summary>
        /// Object that will be converted to JSON and sent to the javascript
        /// </summary>
        public class ReturnData
        {
            public bool IsSuccessful { get; set; }
            public string ErrorMessage { get; set; }
            public string CallingMethod { get; set; }
            public Object Value { get; set; }
        }

        public void ProcessRequest(HttpContext context)
        {


            object returnObject = null;
            ReturnData returnData = new ReturnData();

            try
            {
                if (InsertStoredProcedureName == string.Empty)
                {
                    throw new ArgumentException("Property InsertStoredProcedureName must be set in handler");
                }

                if (GetByIdStoredProcedureName == string.Empty)
                {
                    throw new ArgumentException("Property GetByIdStoredProcedureName must be set in handler");
                }

                if (PreExecution != null)
                {
                    PreExecutionResult result = PreExecution(context);
                    if (result != PreExecutionResult.NoErrors)
                    {
                        throw new PreExecutionFailException(result, "PreExecution Failed");
                    }
                }

                // Get the JSON that was posted
                string json;
                using (Stream st = context.Request.InputStream)
                {

                    byte[] buf = new byte[context.Request.InputStream.Length];
                    int iRead = st.Read(buf, 0, buf.Length);
                    json = Encoding.UTF8.GetString(buf);
                }

                //Deserialize the Javascript
                JavaScriptSerializer ser = new JavaScriptSerializer();
                Dictionary<string, object> ajaxData = (Dictionary<string, object>)ser.Deserialize<Dictionary<string, object>>(json);
                T obj = (T)Activator.CreateInstance(typeof(T), new object[] { });

                //Convert data posted from Javascript to an object of Type T that will be passed into the dataaccess insert method
                foreach (var prop in typeof(T).GetProperties())
                {
                    obj.GetType().GetProperty(prop.Name).SetValue(obj, Convert.ChangeType(ajaxData[prop.Name], prop.PropertyType));
                }

                SqlDataAccessBasic<TID, T> dataAccess = new SqlDataAccessBasic<TID, T>(ConnectionString);
                SqlDataAccessBasic<object, B> dataAccess2 = new SqlDataAccessBasic<object, B>(ConnectionString);
                TID id = dataAccess.Insert(obj, InsertStoredProcedureName);
                returnData.Value = dataAccess2.GetById(id, GetByIdStoredProcedureName);
                returnData.IsSuccessful = true;


                if (BusinessLogic != null)
                {
                    returnObject = BusinessLogic(returnData);
                }
                else
                {
                    returnObject = returnData;
                }


            }
            catch (Exception ex)
            {
                //The child class of this class should implement an ErrorHandling funciton.   
                //Error message should be logged and a more user friendly error message should be sent in the string ErrorMessage
                //CallingMethod should be logged and cleared before being serialized into JSON
                returnData = new ReturnData
                {
                    ErrorMessage = ex.ToString(),
                    CallingMethod = this.GetType().Name,
                    IsSuccessful = false
                };

                if (ErrorHandling != null)
                {
                    returnObject = ErrorHandling(returnData, ex);
                }
                else
                {
                    returnObject = returnData;
                }
            }

            //Convert the results to JSON
            JavaScriptSerializer jss = new JavaScriptSerializer();
            string JSON = jss.Serialize(returnObject);

            //Send JSON string back to browser
            context.Response.ContentType = "application/json";
            context.Response.Write(JSON);
        }
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

    }

}