using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.Data;

public partial class AmsaDbContext : DbContext
{
    public AmsaDbContext()
    {
    }

    public AmsaDbContext(DbContextOptions<AmsaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Level> Levels { get; set; }

    public virtual DbSet<LevelDepartment> LevelDepartments { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<MemberLevelDepartment> MemberLevelDepartments { get; set; }

    public virtual DbSet<National> Nationals { get; set; }

    public virtual DbSet<State> States { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=DefaultConnection");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BEDCF4DB986");

            entity.HasIndex(e => e.DepartmentName, "UQ__Departme__D949CC34646C8B5B").IsUnique();

            entity.Property(e => e.DepartmentName).HasMaxLength(100);
        });

        modelBuilder.Entity<Level>(entity =>
        {
            entity.HasIndex(e => new { e.LevelType, e.NationalId, e.StateId, e.UnitId }, "UQ_Levels_LevelType_Id").IsUnique();

            entity.Property(e => e.LevelType)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.National).WithMany(p => p.Levels)
                .HasForeignKey(d => d.NationalId)
                .HasConstraintName("FK_Levels_National");

            entity.HasOne(d => d.State).WithMany(p => p.Levels)
                .HasForeignKey(d => d.StateId)
                .HasConstraintName("FK_Levels_States");

            entity.HasOne(d => d.Unit).WithMany(p => p.Levels)
                .HasForeignKey(d => d.UnitId)
                .HasConstraintName("FK_Levels_Units");
        });

        modelBuilder.Entity<LevelDepartment>(entity =>
        {
            entity.HasKey(e => e.LevelDepartmentId).HasName("PK__LevelDep__70EB3E688D7838FC");

            entity.HasOne(d => d.Department).WithMany(p => p.LevelDepartments)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LevelDepa__Depar__6FE99F9F");

            entity.HasOne(d => d.Level).WithMany(p => p.LevelDepartments)
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LevelDepartments_Levels");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.MemberId).HasName("PK__tmp_ms_x__0CF04B18844A1307");

            entity.HasIndex(e => e.Phone, "UQ__tmp_ms_x__5C7E359E3AD99E1D").IsUnique();

            entity.HasIndex(e => e.Mkanid, "UQ__tmp_ms_x__706BAAA967D044CC").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__tmp_ms_x__A9D10534C9D85A05").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.Mkanid).HasColumnName("MKANID");
            entity.Property(e => e.Phone).HasMaxLength(15);

            entity.HasOne(d => d.Unit).WithMany(p => p.Members)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Members__UnitId__0C85DE4D");
        });

        modelBuilder.Entity<MemberLevelDepartment>(entity =>
        {
            entity.HasKey(e => e.MemberLevelDepartmentId).HasName("PK__MemberLe__C4CEBAB85CF42426");

            entity.HasOne(d => d.LevelDepartment).WithMany(p => p.MemberLevelDepartments)
                .HasForeignKey(d => d.LevelDepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MemberLev__Level__71D1E811");

            entity.HasOne(d => d.Member).WithMany(p => p.MemberLevelDepartments)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MemberLev__Membe__0B91BA14");
        });

        modelBuilder.Entity<National>(entity =>
        {
            entity.HasKey(e => e.NationalId).HasName("PK__National__E9AA32FB057063F4");

            entity.ToTable("National");

            entity.HasIndex(e => e.NationalName, "UQ__National__1D8A5E87EDFF4DF5").IsUnique();

            entity.Property(e => e.NationalName).HasMaxLength(100);
        });

        modelBuilder.Entity<State>(entity =>
        {
            entity.HasKey(e => e.StateId).HasName("PK__States__C3BA3B3A5778F3FA");

            entity.HasIndex(e => e.StateName, "UQ__States__554763159073B91B").IsUnique();

            entity.Property(e => e.StateName).HasMaxLength(100);

            entity.HasOne(d => d.National).WithMany(p => p.States)
                .HasForeignKey(d => d.NationalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__States__National__60A75C0F");
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.UnitId).HasName("PK__Units__44F5ECB508D31CE9");

            entity.Property(e => e.UnitName).HasMaxLength(100);

            entity.HasOne(d => d.State).WithMany(p => p.Units)
                .HasForeignKey(d => d.StateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Units__StateId__619B8048");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
