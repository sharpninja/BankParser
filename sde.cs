using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PaytonByrd
{
    public class SelfDocumentingException : ApplicationException, IDictionary<string, string>
    {
        #region Constructors
        public SelfDocumentingException() : base()
        {
        }

        public SelfDocumentingException(string message) : base(message)
        {
        }

        public SelfDocumentingException(string message, Exception inner) : base(message, inner)
        {
        }
        #endregion Constructors

        #region Overrides
        public override string ToString()
        {
            return FormatCollectionData() + base.ToString();
        }
        #endregion Overrides

        #region Public
        public virtual void Merge(SelfDocumentingException toMerge)
        {
            foreach (string strKey in toMerge.Keys)
            {
                if (!ContainsKey(strKey))
                {
                    Add(strKey, toMerge[strKey]);
                }
                else if (!ContainsKey("merged_" + toMerge.GetType().FullName + "_" + strKey))
                {
                    Add("merged_" + toMerge.GetType().FullName + "_" + strKey, toMerge[strKey]);
                }
                else
                {
                    throw new ApplicationException("Could not merge exceptions, ambiguous key.");
                }

            }

        }

        public void AddObject(string key, object toAdd)
        {
            if (toAdd == null)
            {
                throw new ArgumentNullException("toAdd", "You must supply a valid object.");
            }
            foreach (PropertyInfo objInfo in toAdd.GetType().GetProperties())
            {
                object objValue = objInfo.GetValue(toAdd, new object[0]);
                this.Add(string.Format(
                    "{0}_{1}.{2}",
                    key,
                    toAdd.GetType().FullName,
                    objInfo.Name), (objValue != null ? objValue.ToString() : "NULL"));
            }
        }
        #endregion Public

        #region Protected
        protected virtual string FormatCollectionData()
        {
            const string STR_DIVIDER = "\n---------------";
            const string STR_DATA_FORMAT = "\n\t {0}:{1}";
            StringBuilder objBuilder = new StringBuilder();
            objBuilder.Append(GetType().FullName);
            objBuilder.Append(STR_DIVIDER);
            foreach (string strKey in Keys)
            {
                objBuilder.AppendFormat(STR_DATA_FORMAT, strKey, this[strKey]);
            }
            objBuilder.AppendLine(STR_DIVIDER);
            return objBuilder.ToString();
        }
        #endregion Protected

        #region IDictionary<string,string> Members
        private Dictionary<string, string> m_objData = new Dictionary<string, string>();

        public void Add(string key, string value)
        {
            m_objData.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return m_objData.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get
            {
                return m_objData.Keys;
            }
        }

        public bool Remove(string key)
        {
            return m_objData.Remove(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            return m_objData.TryGetValue(key, out value);
        }

        public ICollection<string> Values
        {
            get
            {
                return m_objData.Values;
            }
        }

        public string this[string key]
        {
            get
            {
                return m_objData[key];
            }
            set
            {
                m_objData[key] = value;
            }
        }
        #endregion

        #region ICollection<KeyValuePair<string,string>> Members
        public void Add(KeyValuePair<string, string> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            m_objData.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return m_objData.ContainsKey(item.Key) || m_objData.ContainsValue(item.Value);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            int intCounter = arrayIndex;
            foreach (KeyValuePair<string, string> objPair in m_objData)
            {
                array[intCounter++] = objPair;
            }
        }

        public int Count
        {
            get
            {
                return m_objData.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return m_objData.Remove(item.Key);
        }
        #endregion

        #region IEnumerable<KeyValuePair<string,string>> Members
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region IEnumerable Members
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_objData.GetEnumerator();

        }
        #endregion
    }
}
