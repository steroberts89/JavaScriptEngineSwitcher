﻿using OriginalCompatibilityMode = Jurassic.CompatibilityMode;
using OriginalConcatenatedString = Jurassic.ConcatenatedString;
using OriginalJsEngine = Jurassic.ScriptEngine;
using OriginalJsException = Jurassic.JavaScriptException;
using OriginalNull = Jurassic.Null;
using OriginalTypeConverter = Jurassic.TypeConverter;
using OriginalUndefined = Jurassic.Undefined;

namespace JavaScriptEngineSwitcher.Jurassic
{
	using System;

	using Core;
	using CoreStrings = Core.Resources.Strings;

	/// <summary>
	/// Adapter for Jurassic JavaScript engine
	/// </summary>
	public sealed class JurassicJsEngine : JsEngineBase
	{
		/// <summary>
		/// Name of JavaScript engine
		/// </summary>
		private const string ENGINE_NAME = "Jurassic JavaScript engine";

		/// <summary>
		/// Version of original JavaScript engine
		/// </summary>
		private const string ENGINE_VERSION = "Sep 5, 2014";

		/// <summary>
		/// Jurassic JS engine
		/// </summary>
		private OriginalJsEngine _jsEngine;

		/// <summary>
		/// Gets a name of JavaScript engine
		/// </summary>
		public override string Name
		{
			get { return ENGINE_NAME; }
		}

		/// <summary>
		/// Gets a version of original JavaScript engine
		/// </summary>
		public override string Version
		{
			get { return ENGINE_VERSION; }
		}


		/// <summary>
		/// Constructs instance of adapter for Jurassic
		/// </summary>
		public JurassicJsEngine()
		{
			try
			{
				_jsEngine = new OriginalJsEngine
				{
					CompatibilityMode = OriginalCompatibilityMode.Latest,
					EnableExposedClrTypes = true
				};
			}
			catch (Exception e)
			{
				throw new JsEngineLoadException(
					string.Format(CoreStrings.Runtime_JsEngineNotLoaded,
						ENGINE_NAME, e.Message), ENGINE_NAME, ENGINE_VERSION, e);
			}
		}

		/// <summary>
		/// Executes a mapping from the host type to a Jurassic type
		/// </summary>
		/// <param name="value">The source value</param>
		/// <returns>The mapped value</returns>
		private static object MapToJurassicType(object value)
		{
			if (value == null)
			{
				return OriginalNull.Value;
			}

			if (value is Undefined)
			{
				return OriginalUndefined.Value;
			}

			return value;
		}

		/// <summary>
		/// Executes a mapping from the Jurassic type to a host type
		/// </summary>
		/// <param name="value">The source value</param>
		/// <returns>The mapped value</returns>
		private static object MapToHostType(object value)
		{
			if (value is OriginalConcatenatedString)
			{
				return value.ToString();
			}

			if (value is OriginalNull)
			{
				return null;
			}

			if (value is OriginalUndefined)
			{
				return Undefined.Value;
			}

			return value;
		}

		private JsRuntimeException ConvertJavascriptExceptionToJsRuntimeException(
			OriginalJsException jsException)
		{
			var jsRuntimeException = new JsRuntimeException(jsException.Message, ENGINE_NAME, ENGINE_VERSION,
				jsException)
			{
				Category = jsException.Name,
				LineNumber = jsException.LineNumber,
				ColumnNumber = 0,
				SourceFragment = jsException.SourcePath,
				Source = jsException.Source,
				HelpLink = jsException.HelpLink
			};

			return jsRuntimeException;
		}

		#region JsEngineBase implementation

		protected override object InnerEvaluate(string expression)
		{
			object result;

			try
			{
				result = _jsEngine.Evaluate(expression);
			}
			catch (OriginalJsException e)
			{
				throw ConvertJavascriptExceptionToJsRuntimeException(e);
			}

			result = MapToHostType(result);
			
			return result;
		}

		protected override T InnerEvaluate<T>(string expression)
		{
			object result = InnerEvaluate(expression);

			return OriginalTypeConverter.ConvertTo<T>(_jsEngine, result);
		}

		protected override void InnerExecute(string code)
		{
			try
			{
				_jsEngine.Execute(code);
			}
			catch (OriginalJsException e)
			{
				throw ConvertJavascriptExceptionToJsRuntimeException(e);
			}
		}

		protected override object InnerCallFunction(string functionName, params object[] args)
		{
			int argumentCount = args.Length;
			var processedArgs = new object[argumentCount];

			if (argumentCount > 0)
			{
				for (int argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++)
				{
					processedArgs[argumentIndex] = MapToJurassicType(args[argumentIndex]);
				}
			}

			object result;

			try
			{
				result = _jsEngine.CallGlobalFunction(functionName, processedArgs);
			}
			catch (OriginalJsException e)
			{
				throw ConvertJavascriptExceptionToJsRuntimeException(e);
			}

			result = MapToHostType(result);

			return result;
		}

		protected override T InnerCallFunction<T>(string functionName, params object[] args)
		{
			object result = InnerCallFunction(functionName, args);

			return OriginalTypeConverter.ConvertTo<T>(_jsEngine, result);
		}

		protected override bool InnerHasVariable(string variableName)
		{
			bool result = _jsEngine.HasGlobalValue(variableName);
			if (result)
			{
				object value = _jsEngine.GetGlobalValue(variableName);
				result = (value.ToString() != "undefined");
			}

			return result;
		}

		protected override object InnerGetVariableValue(string variableName)
		{
			object result;

			try
			{
				result = _jsEngine.GetGlobalValue(variableName);
			}
			catch (OriginalJsException e)
			{
				throw ConvertJavascriptExceptionToJsRuntimeException(e);
			}

			result = MapToHostType(result);

			return result;
		}

		protected override T InnerGetVariableValue<T>(string variableName)
		{
			object result = InnerGetVariableValue(variableName);

			return OriginalTypeConverter.ConvertTo<T>(_jsEngine, result);
		}

		protected override void InnerSetVariableValue(string variableName, object value)
		{
			object processedValue = MapToJurassicType(value);

			try
			{
				_jsEngine.SetGlobalValue(variableName, processedValue);
			}
			catch (OriginalJsException e)
			{
				throw ConvertJavascriptExceptionToJsRuntimeException(e);
			}
		}

		protected override void InnerRemoveVariable(string variableName)
		{
			InnerSetVariableValue(variableName, Undefined.Value);
		}

		#endregion

		#region IDisposable implementation

		public override void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;

				_jsEngine = null;
			}
		}

		#endregion
	}
}