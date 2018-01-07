using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VirtualPianoPlayer
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}

		/// <summary>
		/// Returns a sub array of an existing array
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
		/// <param name="index"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static T[] SubArray<T>(this T[] data, int index, int length)
		{
			T[] result = new T[length];
			Array.Copy(data, index, result, 0, length);
			return result;
		}

		/// <summary>
		/// Returns a sub array of an existing array
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static T[] SubArray<T>(this T[] data, int index)
		{
			return data.SubArray(index, data.Length - 1);
		}

		/// <summary>
		/// Returns whether the specified object exists inside a given array
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="arr"></param>
		/// <returns></returns>
		public static bool In<T>(this T obj, params T[] arr)
		{
			return arr.Contains(obj);
		}
	}

	[Serializable]
	public class ParseErrorException : Exception
	{
		public ParseErrorException() { }
		public ParseErrorException(string message) : base(message) { }
		public ParseErrorException(string message, Exception inner) : base(message, inner) { }
		protected ParseErrorException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}


	[Serializable]
	public class RuntimeErrorException : Exception
	{
		public RuntimeErrorException() { }
		public RuntimeErrorException(string message) : base(message) { }
		public RuntimeErrorException(string message, Exception inner) : base(message, inner) { }
		protected RuntimeErrorException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
