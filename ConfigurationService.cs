using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorDeErrores.Proxy.Configuration
{
    public class ConfigurationService : ConfigurationSection
    {
        #region Singleton
        private static ConfigurationService _configuration;
        private static object _lock = new object();
        public static ConfigurationService GetConfig
        {
            get
            {
                if (_configuration == null)
                {
                    lock (_lock)
                    {
                        if (_configuration == null)
                        {
                            _configuration = (ConfigurationService)ConfigurationManager.GetSection("windowsService");
                        }
                    }
                }
                return _configuration;
            }
        }

        #endregion
        ConfigurationInterfaceCollection _Interfaces;
        [ConfigurationProperty("endpoints", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ConfigurationInterfaceCollection), AddItemName = "add", ClearItemsName = "clear", RemoveItemName = "remove")]
        public ConfigurationInterfaceCollection Interfaces
        {
            get
            {
                _Interfaces = _Interfaces ?? ((ConfigurationInterfaceCollection)this["endpoints"]);
                return _Interfaces;
            }
        }
    }

    public class ConfigurationInterfaceCollection : ConfigurationElementCollection
    {
        public ConfigurationInterfaceCollection this[int index]
        {
            get { return (ConfigurationInterfaceCollection)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }
        public void Add(ConfigurationInterface serviceConfig)
        {
            BaseAdd(serviceConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ConfigurationInterface();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConfigurationInterface)element).Name;
        }

        public void Remove(ConfigurationInterface serviceConfig)
        {
            BaseRemove(serviceConfig.Name);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public List<ConfigurationInterface> ToList()
        {
            return this.OfType<ConfigurationInterface>().ToList();
        }
    }

    public class ConfigurationInterface : ConfigurationElement
    {
        private string _name;
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get
            {
                return _name ?? (string)this["name"];
            }
            set
            {
                _name = value;
            }
        }

        private string _address;
        [ConfigurationProperty("address", IsRequired = true)]
        public string Address
        {
            get
            {
                return _address ?? (string)this["address"];
            }
            set
            {
                _address = value;
            }
        }

        public Type Type { get; set; }
    }
}
