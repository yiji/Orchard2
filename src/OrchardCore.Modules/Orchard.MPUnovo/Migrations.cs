using Orchard.Data.Migration;
using Orchard.MPUnovo.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orchard.MPUnovo
{
    public class Migrations : DataMigration
    {
        /// <summary>
        /// 应该是系统启动时，定时后台任务会执行此方法
        /// </summary>
        /// <returns></returns>
        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable(nameof(PartnerInfoIndex), table => table
                .Column<string>("UserId")
                .Column<string>("PartnerCode")
            );

            return 1;
        }
    }
}
