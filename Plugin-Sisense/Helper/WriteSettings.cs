using System;
using Pub;

namespace Plugin_Sisense.Helper
{
    public class WriteSettings
    {
        public int CommitSLA { get; set; }
        public Schema Schema { get; set; }
        public ReplicationWriteRequest Replication { get; set; }

        /// <summary>
        /// Returns if mode is set to replication
        /// </summary>
        /// <returns></returns>
        public bool IsReplication()
        {
            return Replication != null;
        }
    }
}