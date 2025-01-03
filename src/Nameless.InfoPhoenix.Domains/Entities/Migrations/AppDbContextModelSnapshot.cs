﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Domains.Entities;

#nullable disable

namespace Nameless.InfoPhoenix.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.0");

            modelBuilder.Entity("Nameless.InfoPhoenix.Data.Document", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasMaxLength(2147483647)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("DocumentDirectoryID")
                        .HasColumnType("TEXT");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasMaxLength(1024)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastIndexingTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastWriteTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("ModifiedAt")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("RawFile")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.HasKey("ID");

                    b.HasIndex("DocumentDirectoryID");

                    b.HasIndex("FilePath")
                        .IsUnique();

                    b.ToTable("Documents", (string)null);
                });

            modelBuilder.Entity("Nameless.InfoPhoenix.Data.DocumentDirectory", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("DirectoryPath")
                        .IsRequired()
                        .HasMaxLength(1024)
                        .HasColumnType("TEXT");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasMaxLength(1024)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastIndexingTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("ModifiedAt")
                        .HasColumnType("TEXT");

                    b.Property<int>("Order")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID");

                    b.HasIndex("DirectoryPath")
                        .IsUnique();

                    b.ToTable("DocumentDirectories", (string)null);
                });

            modelBuilder.Entity("Nameless.InfoPhoenix.Data.Document", b =>
                {
                    b.HasOne("Nameless.InfoPhoenix.Data.DocumentDirectory", null)
                        .WithMany()
                        .HasForeignKey("DocumentDirectoryID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
