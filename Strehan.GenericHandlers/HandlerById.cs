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
    /// Generic handler that calles a stored procdure with a single Id
    /// </summary>
    /// <typeparam name="TID">Type of Id</typeparam>
    public class ByIdHandler<TID> : IHttpHandler
    {
        /// <summary>
        /// Connection String for SQL Server Database
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Stored procedure to be called that takes a single Id parameter
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
        }

        public void ProcessRequest(HttpContext context)
        {


            object returnObject = null;
            ReturnData returnData = new ReturnData();

            try
            {

                if (StoredProcedureName == null || StoredProcedureName == string.Empty)
                {
                    throw new ArgumentException("StoredProcedureName must be set in handler");
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

                TID id = (TID)Convert.ChangeType(ajaxData["Id"], typeof(TID));

              

                SqlDataAccessBasic<TID, object> dataAccess = new SqlDataAccessBasic<TID, object>(ConnectionString);

                dataAccess.IdCall(id, StoredProcedureName);
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