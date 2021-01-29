using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;

namespace Prototype1.Foundation.Web
{
    public class IPAddressRangeFilter
    {
        private readonly List<IPRange> _ignoredRanges = new List<IPRange>();        
        /// <summary>
        /// Class for determining whether or not to ignore a request from a set of ip addresses
        /// Should be configured with the following format "192.168.0.0-192.168.0.255,192.168.1.0-192.168.1.255"
        /// Comma seperated sets of hyphen seperated ranges.
        /// </summary>
        /// <param name="appSettingsKey">The appKey to read the ranges from</param>
        public IPAddressRangeFilter(string appSettingsKey)
        {
            string ipIgnores = ConfigurationManager.AppSettings[appSettingsKey];
            if (!string.IsNullOrEmpty(ipIgnores))
            {
                foreach (string rangeString in ipIgnores.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] ips = rangeString.Split('-');
                    try
                    {
                        uint lower = IPAddressToLongBackwards(ips[0]);
                        uint upper = IPAddressToLongBackwards(ips[1]);
                        IPRange range = new IPRange(lower, upper);
                        _ignoredRanges.Add(range);
                    }
                    catch { }
                }
            }
        }

        public bool Ignore(string ipAddress)
        {
            uint ip = IPAddressToLongBackwards(ipAddress);
            return _ignoredRanges.Any(r => r.Contains(ip));
        }

        static private uint IPAddressToLongBackwards(string IPAddr)
        {
            System.Net.IPAddress oIP = System.Net.IPAddress.Parse(IPAddr);
            byte[] byteIP = oIP.GetAddressBytes();


            uint ip = (uint)byteIP[0] << 24;
            ip += (uint)byteIP[1] << 16;
            ip += (uint)byteIP[2] << 8;
            ip += (uint)byteIP[3];

            return ip;
        }

        private class IPRange
        {
            private readonly uint _lowerBound;
            private readonly uint _upperBound;

            public IPRange(uint lowerBound, uint upperBound)
            {
                _lowerBound = lowerBound;
                _upperBound = upperBound;
            }

            public bool Contains(uint value)
            {
                return _lowerBound <= value && _upperBound >= value;
            }
        }
    }
}
