using System;
using System.IO;

namespace LocalMimu.Repositories;

public static class DbConfig
{
    public static string ConnectionString =>
        $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "localmimu.db")}";
}