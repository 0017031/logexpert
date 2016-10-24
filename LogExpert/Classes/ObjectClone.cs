﻿using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace LogExpert
{
	/*
	 * By - Rahul Dantkale
	 * Company - Indigo Architects
	 * 
	 */
	public static class ObjectClone
	{
		public static T Clone<T>(T RealObject)
		{
			using (Stream objectStream = new MemoryStream())
			{
				IFormatter formatter = new BinaryFormatter();
				formatter.Serialize(objectStream, RealObject);
				objectStream.Seek(0, SeekOrigin.Begin);
				return (T)formatter.Deserialize(objectStream);
			}
		}
	}
}