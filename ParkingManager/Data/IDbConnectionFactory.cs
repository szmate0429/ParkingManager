using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace ParkingManager.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
