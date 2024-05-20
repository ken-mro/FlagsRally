using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Repository;
public static class Constants
{
    private const string DATABSE_NAME = "FlagsRally.db3";
    public static string DatabaseName => DATABSE_NAME;
    public static string DataBasePath => Path.Combine(FileSystem.AppDataDirectory, DATABSE_NAME);
}