/*
 Copyright (C) 2018 William Strehan
*/

using System;

namespace Strehan.GenericHandlers
{
    /// <summary>
    /// Thrown when an error occurrs when the PreExecution delegate is run
    /// </summary>
    /// <typeparam name="TID">Id Type of record that could not be found in the database</typeparam>
    [Serializable]
    public class PreExecutionFailException : Exception
    {
        /// <summary>
        /// Represents Result of PreExecution delegate
        /// </summary>
        public PreExecutionResult PreExecutionResult { get; private set; }

        /// <summary>
        /// Creates an PreExecutionFailException
        /// </summary>
        /// <param name="result">PreExecutionResult enumeration</param>
        /// <param name="message"></param>
        public PreExecutionFailException(PreExecutionResult result, string message) : base(message)
        {
            this.PreExecutionResult = result;
        }

        /// <summary>
        /// Added the PreExecutionResult in ToString()
        /// </summary>
        /// <returns>Verbose data for Logging</returns>
        public override string ToString()
        {
            return string.Format(base.ToString() + ", PreExecutionResult: {0}", this.PreExecutionResult.ToString());
        }
    }
}
