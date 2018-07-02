/*
 Copyright (C) 2018 William Strehan
*/

using System;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Web;
using Strehan.DataAccess;



namespace Strehan.GenericHandlers
{

    /// <summary>
    /// Summary description for GetListHandler
    /// </summary>
    public class ListHandler<T> : IHttpHandler
    {
        /// <summary>
        /// Connection String for SQL Server Database
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Stored procedure for handler to use
        /// </summary>
        public string StoredProcedureName { get; set; }

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
            public List<T> List { get; set; }
        }

        public void ProcessRequest(HttpContext context)
        {
            

            // Make sure new results are fetched every time
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);

            object returnObject = null;
            ReturnData returnData;

            try
            {
                if (StoredProcedureName == string.Empty)
                {
                    throw new ArgumentException("Property StoredProcedureName must be set in handler");
                }

                if (PreExecution != null)
                {
                    PreExecutionResult result = PreExecution(context);
                    if (result != PreExecutionResult.NoErrors)
                    {
                        throw new PreExecutionFailException(result, "PreExecution Failed");
                    }
                }

                SqlDataAccessBasic<object, T> dataAccess = new SqlDataAccessBasic<object, T>(ConnectionString);

                returnData = new ReturnData
                {
                    List = dataAccess.GetList(StoredProcedureName),
                    IsSuccessful = true
                };

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