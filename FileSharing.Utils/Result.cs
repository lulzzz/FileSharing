﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Utils
{
    public class Result
    {
        public bool Success { get; }
        public bool Failure => !Success;
        public string Error { get; }

        protected Result(bool success, string error)
        {
            this.Success = success;
            this.Error = error;
        }

        public static Result Fail(string message)
        {
            return new Result(false, message);
        }

        public static Result<T> Fail<T>(string message)
        {
            return new Result<T>(default(T), false, message);
        }

        public static Result Ok()
        {
            return new Result(true, string.Empty);
        }

        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(value, true, string.Empty);
        }

        public static Result Combine(params Result[] results)
        {
            foreach (Result result in results)
            {
                if (result.Failure)
                {
                    return result;
                }
            }

            return Ok();
        }
    }

    public class Result<T>: Result
    {
        public T Value { get; }
        protected internal Result(T value, bool success, string error):
            base (success, error)
        {
            this.Value = value;
        }
    }
}
