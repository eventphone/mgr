﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using epmgr.Data;

#nullable disable

namespace epmgr.Migrations
{
    [DbContext(typeof(MgrDbContext))]
    partial class MgrDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "mgr_extension_type", new[] { "dect", "sip", "premium", "group", "special", "gsm", "app", "announcement" });
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("epmgr.Data.MgrDesasterCall", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Announcement")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("Name")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("Pin")
                        .HasMaxLength(6)
                        .HasColumnType("character varying(6)");

                    b.Property<string>("Target")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.HasKey("Id");

                    b.HasIndex("Pin")
                        .IsUnique();

                    b.ToTable("DesasterCall", (string)null);
                });

            modelBuilder.Entity("epmgr.Data.MgrExtension", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("DeleteAfterResync")
                        .HasColumnType("boolean");

                    b.Property<string>("Extension")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.Property<string>("Language")
                        .HasMaxLength(5)
                        .HasColumnType("character varying(5)");

                    b.Property<string>("Name")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Password")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.Property<string>("Token")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.Property<MgrExtensionType>("Type")
                        .HasColumnType("mgr_extension_type");

                    b.Property<bool>("UseEncryption")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("Extension")
                        .IsUnique();

                    b.ToTable("Extension", (string)null);
                });

            modelBuilder.Entity("epmgr.Data.MgrMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Error")
                        .HasColumnType("text");

                    b.Property<bool>("Failed")
                        .HasColumnType("boolean");

                    b.Property<string>("Json")
                        .HasColumnType("jsonb");

                    b.Property<long>("Timestamp")
                        .HasColumnType("bigint");

                    b.Property<string>("Type")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Failed");

                    b.HasIndex("Timestamp");

                    b.ToTable("MessageQueue", (string)null);
                });

            modelBuilder.Entity("epmgr.Data.MgrUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("LastLogon")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("PasswordHash")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("Username")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.HasKey("Id");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("User", (string)null);

                    b.HasData(
                        new
                        {
                            Id = 1,
                            LastLogon = new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                            PasswordHash = "AQAAAAEAACcQAAAAEB8YUrE2oHquTwLo4LGsvEmN10u7KUDg/AMreQ2sQLqfA2ln0hCaic1Grcc/qQ5+ew==",
                            Username = "mgr"
                        });
                });
#pragma warning restore 612, 618
        }
    }
}
