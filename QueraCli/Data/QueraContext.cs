using Microsoft.EntityFrameworkCore;
using QueraCli.Models;

namespace QueraCli.Data;

public class QueraContext : DbContext {
    public DbSet<ConfigDb> Configs { get; set; } = null!;

    public string DbPath { get; }

    public QueraContext() {
        if (!Directory.Exists(AppSetting.ProgramDirectory))
            Directory.CreateDirectory(AppSetting.ProgramDirectory);
        DbPath = AppSetting.DbPath;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}