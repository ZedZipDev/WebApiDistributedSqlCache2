using System;
using System.Collections.Generic;

namespace WebApiDistributedSqlCache.Model
{
    [Serializable]
    public class SubTypeObject
    {
        public Int32 SubType;
        public Int32 Type;
        public string Name;
        public SubTypeObject()
        {

        }

    }
    public class SubTypeObjects : ICacheable
    {
        public List<SubTypeObject> SubTypes = null;
         public static string GetCacheKey()
        {
            return CinegyCacheType.SubTypesInfo.ToString();
        }

        string ICacheable.GetCacheKey()
        {
            return SubTypeObjects.GetCacheKey();
        }
    }

    public interface ICacheable
    {
        string GetCacheKey();
    }

    public enum CinegyCacheType
    {
        SubTypesInfo,
        FileSet
    }
}
