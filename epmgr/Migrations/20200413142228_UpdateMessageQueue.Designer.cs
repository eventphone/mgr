﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using epmgr.Data;

namespace epmgr.Migrations
{
    [DbContext(typeof(MgrDbContext))]
    [Migration("20200413142228_UpdateMessageQueue")]
    partial class UpdateMessageQueue
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:Enum:mgr_extension_type", "dect,sip,premium,group,special,gsm,app")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("epmgr.Data.MgrExtension", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("DeleteAfterResync")
                        .HasColumnType("boolean");

                    b.Property<string>("Extension")
                        .HasColumnType("character varying(32)")
                        .HasMaxLength(32);

                    b.Property<string>("Language")
                        .HasColumnType("character varying(5)")
                        .HasMaxLength(5);

                    b.Property<string>("Name")
                        .HasColumnType("character varying(64)")
                        .HasMaxLength(64);

                    b.Property<string>("Password")
                        .HasColumnType("character varying(32)")
                        .HasMaxLength(32);

                    b.Property<string>("Token")
                        .HasColumnType("character varying(32)")
                        .HasMaxLength(32);

                    b.Property<MgrExtensionType>("Type")
                        .HasColumnType("mgr_extension_type");

                    b.Property<bool>("UseEncryption")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("Extension")
                        .IsUnique();

                    b.ToTable("Extension");
                });

            modelBuilder.Entity("epmgr.Data.MgrMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

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

                    b.ToTable("MessageQueue");
                });

            modelBuilder.Entity("epmgr.Data.MgrPrinter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("IpAddress")
                        .IsRequired()
                        .HasColumnType("character varying(255)")
                        .HasMaxLength(255);

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Printer");
                });

            modelBuilder.Entity("epmgr.Data.MgrUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTimeOffset>("LastLogon")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("character varying(255)")
                        .HasMaxLength(255);

                    b.Property<int?>("PrinterId")
                        .HasColumnType("integer");

                    b.Property<string>("Username")
                        .HasColumnType("character varying(64)")
                        .HasMaxLength(64);

                    b.HasKey("Id");

                    b.HasIndex("PrinterId");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("User");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            LastLogon = new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                            PasswordHash = "AQAAAAEAACcQAAAAEB8YUrE2oHquTwLo4LGsvEmN10u7KUDg/AMreQ2sQLqfA2ln0hCaic1Grcc/qQ5+ew==",
                            Username = "mgr"
                        });
                });

            modelBuilder.Entity("epmgr.Data.MgrUser", b =>
                {
                    b.HasOne("epmgr.Data.MgrPrinter", "Printer")
                        .WithMany()
                        .HasForeignKey("PrinterId");
                });
#pragma warning restore 612, 618
        }
    }
}
