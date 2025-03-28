﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.EntityFrameworkCore.Query;

#nullable disable

public abstract class AdHocJsonQueryRelationalTestBase : AdHocJsonQueryTestBase
{
    #region 21006

    public override async Task Project_missing_required_navigation(bool async)
    {
        var message = (await Assert.ThrowsAsync<InvalidOperationException>(
            () => base.Project_missing_required_navigation(async))).Message;

        Assert.Equal(RelationalStrings.JsonRequiredEntityWithNullJson(typeof(Context21006.JsonEntityNested).Name), message);
    }

    public override async Task Project_null_required_navigation(bool async)
    {
        var message = (await Assert.ThrowsAsync<InvalidOperationException>(
            () => base.Project_null_required_navigation(async))).Message;

        Assert.Equal(RelationalStrings.JsonRequiredEntityWithNullJson(typeof(Context21006.JsonEntityNested).Name), message);
    }

    public override async Task Project_top_level_entity_with_null_value_required_scalars(bool async)
    {
        var message = (await Assert.ThrowsAsync<InvalidOperationException>(
            () => base.Project_top_level_entity_with_null_value_required_scalars(async))).Message;

        Assert.Equal("Cannot get the value of a token type 'Null' as a number.", message);
    }

    protected override void OnModelCreating21006(ModelBuilder modelBuilder)
    {
        base.OnModelCreating21006(modelBuilder);

        modelBuilder.Entity<Context21006.Entity>(
            b =>
            {
                b.ToTable("Entities");
                b.OwnsOne(x => x.OptionalReference).ToJson();
                b.OwnsOne(x => x.RequiredReference).ToJson();
                b.OwnsMany(x => x.Collection).ToJson();
            });
    }

    #endregion

    #region 32310

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Contains_on_nested_collection_with_init_only_navigation(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            onModelCreating: b => b.Entity<Pub32310>().OwnsOne(e => e.Visits).ToJson().HasColumnType(JsonColumnType),
            seed: Seed32310);

        await using var context = contextFactory.CreateContext();

        var query = context.Set<Pub32310>()
            .Where(u => u.Visits.DaysVisited.Contains(new DateOnly(2023, 1, 1)));

        var result = async
            ? await query.FirstOrDefaultAsync()!
            : query.FirstOrDefault()!;

        Assert.Equal("FBI", result.Name);
        Assert.Equal(new DateOnly(2023, 1, 1), result.Visits.DaysVisited.Single());
    }

    protected virtual async Task Seed32310(DbContext context)
    {
        var user = new Pub32310
        {
            Name = "FBI",
            Visits = new Visits32310 { LocationTag = "tag", DaysVisited = [new DateOnly(2023, 1, 1)] }
        };

        context.Add(user);
        await context.SaveChangesAsync();
    }

    public class Pub32310
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public Visits32310 Visits { get; set; } = null!;
    }

    public class Visits32310
    {
        public string LocationTag { get; set; }
        public required List<DateOnly> DaysVisited { get; init; }
    }

    #endregion

    #region 29219

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Optional_json_properties_materialized_as_null_when_the_element_in_json_is_not_present(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModel29219,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: Seed29219);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntity29219>().Where(x => x.Id == 3);

            var result = async
                ? await query.SingleAsync()
                : query.Single();

            Assert.Equal(3, result.Id);
            Assert.Null(result.Reference.NullableScalar);
            Assert.Null(result.Collection[0].NullableScalar);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Can_project_nullable_json_property_when_the_element_in_json_is_not_present(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModel29219,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: Seed29219);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntity29219>().OrderBy(x => x.Id).Select(x => x.Reference.NullableScalar);

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal(11, result[0]);
            Assert.Null(result[1]);
            Assert.Null(result[2]);
        }
    }

    protected void BuildModel29219(ModelBuilder modelBuilder)
        => modelBuilder.Entity<MyEntity29219>(
            b =>
            {
                b.ToTable("Entities");
                b.Property(x => x.Id).ValueGeneratedNever();
                b.OwnsOne(x => x.Reference).ToJson().HasColumnType(JsonColumnType);
                b.OwnsMany(x => x.Collection).ToJson().HasColumnType(JsonColumnType);
            });

    protected abstract Task Seed29219(DbContext ctx);

    public class MyEntity29219
    {
        public int Id { get; set; }
        public MyJsonEntity29219 Reference { get; set; }
        public List<MyJsonEntity29219> Collection { get; set; }
    }

    public class MyJsonEntity29219
    {
        public int NonNullableScalar { get; set; }
        public int? NullableScalar { get; set; }
    }

    #endregion

    #region 30028

    protected abstract Task Seed30028(DbContext ctx);

    protected virtual void BuildModel30028(ModelBuilder modelBuilder)
        => modelBuilder.Entity<MyEntity30028>(
            b =>
            {
                b.Property(x => x.Id).ValueGeneratedNever();
                b.ToTable("Entities");
                b.OwnsOne(
                    x => x.Json, nb =>
                    {
                        nb.ToJson().HasColumnType(JsonColumnType);
                        nb.OwnsMany(x => x.Collection, nnb => nnb.OwnsOne(x => x.Nested));
                        nb.OwnsOne(x => x.OptionalReference, nnb => nnb.OwnsOne(x => x.Nested));
                        nb.OwnsOne(x => x.RequiredReference, nnb => nnb.OwnsOne(x => x.Nested));
                        nb.Navigation(x => x.RequiredReference).IsRequired();
                    });
            });

    public class MyEntity30028
    {
        public int Id { get; set; }
        public MyJsonRootEntity30028 Json { get; set; }
    }

    public class MyJsonRootEntity30028
    {
        public string RootName { get; set; }
        public MyJsonBranchEntity30028 RequiredReference { get; set; }
        public MyJsonBranchEntity30028 OptionalReference { get; set; }
        public List<MyJsonBranchEntity30028> Collection { get; set; }
    }

    public class MyJsonBranchEntity30028
    {
        public string BranchName { get; set; }
        public MyJsonLeafEntity30028 Nested { get; set; }
    }

    public class MyJsonLeafEntity30028
    {
        public string LeafName { get; set; }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Accessing_missing_navigation_works(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModel30028,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: Seed30028);

        using (var context = contextFactory.CreateContext())
        {
            var result = context.Set<MyEntity30028>().OrderBy(x => x.Id).ToList();
            Assert.Equal(4, result.Count);
            Assert.NotNull(result[0].Json.Collection);
            Assert.NotNull(result[0].Json.OptionalReference);
            Assert.NotNull(result[0].Json.RequiredReference);

            Assert.Null(result[1].Json.Collection);
            Assert.NotNull(result[1].Json.OptionalReference);
            Assert.NotNull(result[1].Json.RequiredReference);

            Assert.NotNull(result[2].Json.Collection);
            Assert.Null(result[2].Json.OptionalReference);
            Assert.NotNull(result[2].Json.RequiredReference);

            Assert.NotNull(result[3].Json.Collection);
            Assert.NotNull(result[3].Json.OptionalReference);
            Assert.Null(result[3].Json.RequiredReference);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Missing_navigation_works_with_deduplication(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModel30028,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: Seed30028);

        using (var context = contextFactory.CreateContext())
        {
            var queryable = context.Set<MyEntity30028>().OrderBy(x => x.Id).Select(
                x => new
                {
                    x,
                    x.Json,
                    x.Json.OptionalReference,
                    x.Json.RequiredReference,
                    NestedOptional = x.Json.OptionalReference.Nested,
                    NestedRequired = x.Json.RequiredReference.Nested,
                    x.Json.Collection,
                }).AsNoTracking();

            var result = async ? await queryable.ToListAsync() : queryable.ToList();

            Assert.Equal(4, result.Count);
            Assert.NotNull(result[0].OptionalReference);
            Assert.NotNull(result[0].RequiredReference);
            Assert.NotNull(result[0].NestedOptional);
            Assert.NotNull(result[0].NestedRequired);
            Assert.NotNull(result[0].Collection);

            Assert.NotNull(result[1].OptionalReference);
            Assert.NotNull(result[1].RequiredReference);
            Assert.NotNull(result[1].NestedOptional);
            Assert.NotNull(result[1].NestedRequired);
            Assert.Null(result[1].Collection);

            Assert.Null(result[2].OptionalReference);
            Assert.NotNull(result[2].RequiredReference);
            Assert.Null(result[2].NestedOptional);
            Assert.NotNull(result[2].NestedRequired);
            Assert.NotNull(result[2].Collection);

            Assert.NotNull(result[3].OptionalReference);
            Assert.Null(result[3].RequiredReference);
            Assert.NotNull(result[3].NestedOptional);
            Assert.Null(result[3].NestedRequired);
            Assert.NotNull(result[3].Collection);
        }
    }

    #endregion

    #region 32939

    [ConditionalFact]
    public virtual async Task Project_json_with_no_properties()
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModel32939,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: Seed32939);

        using var context = contextFactory.CreateContext();
        context.Set<Entity32939>().ToList();
    }

    protected Task Seed32939(DbContext ctx)
    {
        var entity = new Entity32939 { Empty = new JsonEmpty32939(), FieldOnly = new JsonFieldOnly32939() };

        ctx.Add(entity);
        return ctx.SaveChangesAsync();
    }

    protected virtual void BuildModel32939(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entity32939>().Property(x => x.Id).ValueGeneratedNever();
        modelBuilder.Entity<Entity32939>().OwnsOne(x => x.Empty, b => b.ToJson().HasColumnType(JsonColumnType));
        modelBuilder.Entity<Entity32939>().OwnsOne(x => x.FieldOnly, b => b.ToJson().HasColumnType(JsonColumnType));
    }

    public class Entity32939
    {
        public int Id { get; set; }
        public JsonEmpty32939 Empty { get; set; }
        public JsonFieldOnly32939 FieldOnly { get; set; }
    }

    public class JsonEmpty32939
    {
    }

    public class JsonFieldOnly32939
    {
        public int Field;
    }

    #endregion

    #region 33046

    protected abstract Task Seed33046(DbContext ctx);

    [ConditionalFact]
    public virtual async Task Query_with_nested_json_collection_mapped_to_private_field_via_IReadOnlyList()
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModel33046,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: Seed33046);

        using var context = contextFactory.CreateContext();
        var query = context.Set<Review>().ToList();
        Assert.Equal(1, query.Count);
    }

    protected virtual void BuildModel33046(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Review>(
            b =>
            {
                b.ToTable("Reviews");
                b.Property(x => x.Id).ValueGeneratedNever();
                b.OwnsMany(
                    x => x.Rounds, ownedBuilder =>
                    {
                        ownedBuilder.ToJson().HasColumnType(JsonColumnType);
                        ownedBuilder.OwnsMany(r => r.SubRounds);
                    });
            });

    public class Review
    {
        public int Id { get; set; }

#pragma warning disable IDE0044 // Add readonly modifier
        private List<ReviewRound> _rounds = [];
#pragma warning restore IDE0044 // Add readonly modifier
        public IReadOnlyList<ReviewRound> Rounds
            => _rounds.AsReadOnly();
    }

    public class ReviewRound
    {
        public int RoundNumber { get; set; }

#pragma warning disable IDE0044 // Add readonly modifier
        private readonly List<SubRound> _subRounds = [];
#pragma warning restore IDE0044 // Add readonly modifier
        public IReadOnlyList<SubRound> SubRounds
            => _subRounds.AsReadOnly();
    }

    public class SubRound
    {
        public int SubRoundNumber { get; set; }
    }

    #endregion

    #region 34293

    [ConditionalFact]
    public virtual async Task Project_entity_with_optional_json_entity_owned_by_required_json()
    {
        var contextFactory = await InitializeAsync<Context34293>(
            onModelCreating: OnModelCreating34293,
            seed: ctx => ctx.Seed());

        using var context = contextFactory.CreateContext();
        var entityProjection = await context.Set<Context34293.Entity>().ToListAsync();

        Assert.Equal(3, entityProjection.Count);
    }

    [ConditionalFact]
    public virtual async Task Project_required_json_entity()
    {
        var contextFactory = await InitializeAsync<Context34293>(
            onModelCreating: OnModelCreating34293,
            seed: ctx => ctx.Seed());

        using var context = contextFactory.CreateContext();

        var rootProjection = await context.Set<Context34293.Entity>().AsNoTracking().Where(x => x.Id != 3).Select(x => x.Json).ToListAsync();
        Assert.Equal(2, rootProjection.Count);

        var branchProjection = await context.Set<Context34293.Entity>().AsNoTracking().Where(x => x.Id != 3).Select(x => x.Json.Required).ToListAsync();
        Assert.Equal(2, rootProjection.Count);

        var badRootProjectionMessage = (await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Set<Context34293.Entity>().AsNoTracking().Where(x => x.Id == 3).Select(x => x.Json).ToListAsync())).Message;
        Assert.Equal(RelationalStrings.JsonRequiredEntityWithNullJson(nameof(Context34293.JsonBranch)), badRootProjectionMessage);

        var badBranchProjectionMessage = (await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Set<Context34293.Entity>().AsNoTracking().Where(x => x.Id == 3).Select(x => x.Json.Required).ToListAsync())).Message;
        Assert.Equal(RelationalStrings.JsonRequiredEntityWithNullJson(nameof(Context34293.JsonBranch)), badBranchProjectionMessage);
    }

    [ConditionalFact]
    public virtual async Task Project_optional_json_entity_owned_by_required_json_entity()
    {
        var contextFactory = await InitializeAsync<Context34293>(
            onModelCreating: OnModelCreating34293,
            seed: ctx => ctx.Seed());

        using var context = contextFactory.CreateContext();
        var leafProjection = await context.Set<Context34293.Entity>().AsNoTracking().Select(x => x.Json.Required.Optional).ToListAsync();
        Assert.Equal(3, leafProjection.Count);
    }

    protected class Context34293(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Entity> Entities { get; set; }

        public class Entity
        {
            public int Id { get; set; }
            public JsonRoot Json { get; set; }
        }

        public class JsonRoot
        {
            public DateTime Date { get; set; }

            public JsonBranch Required { get; set; }
        }

        public class JsonBranch
        {
            public int Number { get; set; }
            public JsonLeaf Optional { get; set; }
        }

        public class JsonLeaf
        {
            public string Name { get; set; }
        }

        public async Task Seed()
        {
            // everything - ok
            var e1 = new Entity
            {
                Id = 1,
                Json = new JsonRoot
                {
                    Date = new DateTime(2001, 1, 1),
                    Required = new JsonBranch
                    {
                        Number = 1,
                        Optional = new JsonLeaf { Name = "optional 1" }
                    }
                }
            };

            // null leaf - ok (optional nav)
            var e2 = new Entity
            {
                Id = 2,
                Json = new JsonRoot
                {
                    Date = new DateTime(2002, 2, 2),
                    Required = new JsonBranch
                    {
                        Number = 2,
                        Optional = null
                    }
                }
            };

            // null branch - invalid (required nav)
            var e3 = new Entity
            {
                Id = 3,
                Json = new JsonRoot
                {
                    Date = new DateTime(2003, 3, 3),
                    Required = null,
                }
            };

            Entities.AddRange(e1, e2, e3);
            await SaveChangesAsync();
        }
    }

    protected virtual void OnModelCreating34293(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Context34293.Entity>(
            b =>
            {
                b.Property(x => x.Id).ValueGeneratedNever();
                b.OwnsOne(
                    x => x.Json, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.OwnsOne(x => x.Required, bb =>
                        {
                            bb.OwnsOne(x => x.Optional);
                            bb.Navigation(x => x.Optional).IsRequired(false);
                        });
                        b.Navigation(x => x.Required).IsRequired(true);
                    });
                b.Navigation(x => x.Json).IsRequired(true);
            });
    }

    #endregion

    #region 34960

    [ConditionalFact]
    public virtual async Task Project_entity_with_json_null_values()
    {
        var contextFactory = await InitializeAsync<Context34960>(seed: Seed34960, onModelCreating: OnModelCreating34960);

        using var context = contextFactory.CreateContext();
        var query = await context.Entities.ToListAsync();
    }

    [ConditionalFact]
    public virtual async Task Try_project_collection_but_JSON_is_entity()
    {
        var contextFactory = await InitializeAsync<Context34960>(seed: Seed34960, onModelCreating: OnModelCreating34960);
        using var context = contextFactory.CreateContext();

        Assert.Equal(
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.Junk.AsNoTracking().Where(x => x.Id == 1).Select(x => x.Collection).FirstOrDefaultAsync())).Message,
            CoreStrings.JsonReaderInvalidTokenType(nameof(JsonTokenType.StartObject)));
    }

    [ConditionalFact]
    public virtual async Task Try_project_reference_but_JSON_is_collection()
    {
        var contextFactory = await InitializeAsync<Context34960>(seed: Seed34960, onModelCreating: OnModelCreating34960);
        using var context = contextFactory.CreateContext();

        Assert.Equal(
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.Junk.AsNoTracking().Where(x => x.Id == 2).Select(x => x.Reference).FirstOrDefaultAsync())).Message,
            CoreStrings.JsonReaderInvalidTokenType(nameof(JsonTokenType.StartArray)));
    }

    protected class Context34960(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Entity> Entities { get; set; }
        public DbSet<JunkEntity> Junk { get; set; }

        public class Entity
        {
            public int Id { get; set; }
            public JsonEntity Reference { get; set; }
            public List<JsonEntity> Collection { get; set; }
        }

        public class JsonEntity
        {
            public string Name { get; set; }
            public double Number { get; set; }

            public JsonEntityNested NestedReference { get; set; }
            public List<JsonEntityNested> NestedCollection { get; set; }
        }

        public class JsonEntityNested
        {
            public DateTime DoB { get; set; }
            public string Text { get; set; }
        }

        public class JunkEntity
        {
            public int Id { get; set; }
            public JsonEntity Reference { get; set; }
            public List<JsonEntity> Collection { get; set; }
        }
    }

    protected virtual void OnModelCreating34960(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Context34960.Entity>(
            b =>
            {
                b.ToTable("Entities");
                b.Property(x => x.Id).ValueGeneratedNever();

                b.OwnsOne(
                    x => x.Reference, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.OwnsOne(x => x.NestedReference);
                        b.OwnsMany(x => x.NestedCollection);
                    });

                b.OwnsMany(
                    x => x.Collection, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.OwnsOne(x => x.NestedReference);
                        b.OwnsMany(x => x.NestedCollection);
                    });
            });

        modelBuilder.Entity<Context34960.JunkEntity>(
            b =>
            {
                b.ToTable("Junk");
                b.Property(x => x.Id).ValueGeneratedNever();

                b.OwnsOne(
                    x => x.Reference, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.Ignore(x => x.NestedReference);
                        b.Ignore(x => x.NestedCollection);
                    });

                b.OwnsMany(
                    x => x.Collection, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.Ignore(x => x.NestedReference);
                        b.Ignore(x => x.NestedCollection);
                    });
            });
    }

    protected virtual async Task Seed34960(Context34960 ctx)
    {
        // everything
        var e1 = new Context34960.Entity
        {
            Id = 1,
            Reference = new Context34960.JsonEntity
            {
                Name = "ref1",
                Number = 1.5f,
                NestedReference = new Context34960.JsonEntityNested
                {
                    DoB = new DateTime(2000, 1, 1),
                    Text = "nested ref 1"
                },
                NestedCollection =
                [
                    new Context34960.JsonEntityNested
                    {
                        DoB = new DateTime(2001, 1, 1),
                        Text = "nested col 1 1"
                    },
                    new Context34960.JsonEntityNested
                    {
                        DoB = new DateTime(2001, 2, 2),
                        Text = "nested col 1 2"
                    },
                ],
            },

            Collection =
            [
                new Context34960.JsonEntity
                {
                    Name = "col 1 1",
                    Number = 2.5f,
                    NestedReference = new Context34960.JsonEntityNested
                    {
                        DoB = new DateTime(2010, 1, 1),
                        Text = "nested col 1 1 ref 1"
                    },
                    NestedCollection =
                    [
                        new Context34960.JsonEntityNested
                        {
                            DoB = new DateTime(2011, 1, 1),
                            Text = "nested col 1 1 col 1 1"
                        },
                        new Context34960.JsonEntityNested
                        {
                            DoB = new DateTime(2011, 2, 2),
                            Text = "nested col 1 1 col 1 2"
                        },
                    ],
                },
                new Context34960.JsonEntity
                {
                    Name = "col 1 2",
                    Number = 2.5f,
                    NestedReference = new Context34960.JsonEntityNested
                    {
                        DoB = new DateTime(2020, 1, 1),
                        Text = "nested col 1 2 ref 1"
                    },
                    NestedCollection =
                    [
                        new Context34960.JsonEntityNested
                        {
                            DoB = new DateTime(2021, 1, 1),
                            Text = "nested col 1 2 col 1 1"
                        },
                        new Context34960.JsonEntityNested
                        {
                            DoB = new DateTime(2021, 2, 2),
                            Text = "nested col 1 2 col 1 2"
                        },
                    ],
                },
            ],
        };

        // relational nulls
        var e2 = new Context34960.Entity
        {
            Id = 2,
            Reference = null,
            Collection = null
        };

        // nested relational nulls
        var e3 = new Context34960.Entity
        {
            Id = 3,
            Reference = new Context34960.JsonEntity
            {
                Name = "ref3",
                Number = 3.5f,
                NestedReference = null,
                NestedCollection = null
            },

            Collection =
            [
                new Context34960.JsonEntity
                {
                    Name = "col 3 1",
                    Number = 32.5f,
                    NestedReference = null,
                    NestedCollection = null,
                },
                new Context34960.JsonEntity
                {
                    Name = "col 3 2",
                    Number = 33.5f,
                    NestedReference = null,
                    NestedCollection = null,
                },
            ],
        };

        ctx.Entities.AddRange(e1, e2, e3);
        await ctx.SaveChangesAsync();
    }

    #endregion

    #region ArrayOfPrimitives

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Project_json_array_of_primitives_on_reference(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelArrayOfPrimitives,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedArrayOfPrimitives);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityArrayOfPrimitives>().OrderBy(x => x.Id)
                .Select(x => new { x.Reference.IntArray, x.Reference.ListOfString });

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(3, result[0].IntArray.Length);
            Assert.Equal(3, result[0].ListOfString.Count);
            Assert.Equal(3, result[1].IntArray.Length);
            Assert.Equal(3, result[1].ListOfString.Count);
        }
    }

    [ConditionalTheory(Skip = "Issue #32611")]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Project_json_array_of_primitives_on_collection(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelArrayOfPrimitives,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedArrayOfPrimitives);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityArrayOfPrimitives>().OrderBy(x => x.Id)
                .Select(x => new { x.Collection[0].IntArray, x.Collection[1].ListOfString });

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(3, result[0].IntArray.Length);
            Assert.Equal(2, result[0].ListOfString.Count);
            Assert.Equal(3, result[1].IntArray.Length);
            Assert.Equal(2, result[1].ListOfString.Count);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Project_element_of_json_array_of_primitives(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelArrayOfPrimitives,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedArrayOfPrimitives);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityArrayOfPrimitives>().OrderBy(x => x.Id).Select(
                x => new { ArrayElement = x.Reference.IntArray[0], ListElement = x.Reference.ListOfString[1] });

            var result = async
                ? await query.ToListAsync()
                : query.ToList();
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Predicate_based_on_element_of_json_array_of_primitives1(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelArrayOfPrimitives,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedArrayOfPrimitives);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityArrayOfPrimitives>().Where(x => x.Reference.IntArray[0] == 1);

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(1, result[0].Reference.IntArray[0]);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Predicate_based_on_element_of_json_array_of_primitives2(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelArrayOfPrimitives,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedArrayOfPrimitives);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityArrayOfPrimitives>().Where(x => x.Reference.ListOfString[1] == "Bar");

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal("Bar", result[0].Reference.ListOfString[1]);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Predicate_based_on_element_of_json_array_of_primitives3(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelArrayOfPrimitives,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedArrayOfPrimitives);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityArrayOfPrimitives>().Where(
                    x => x.Reference.IntArray.AsQueryable().ElementAt(0) == 1
                        || x.Reference.ListOfString.AsQueryable().ElementAt(1) == "Bar")
                .OrderBy(e => e.Id);

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(1, result[0].Reference.IntArray[0]);
            Assert.Equal("Bar", result[0].Reference.ListOfString[1]);
        }
    }

    protected abstract Task SeedArrayOfPrimitives(DbContext ctx);

    protected virtual void BuildModelArrayOfPrimitives(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MyEntityArrayOfPrimitives>().Property(x => x.Id).ValueGeneratedNever();
        modelBuilder.Entity<MyEntityArrayOfPrimitives>().OwnsOne(
            x => x.Reference, b => b.ToJson().HasColumnType(JsonColumnType));

        modelBuilder.Entity<MyEntityArrayOfPrimitives>().OwnsMany(
            x => x.Collection, b => b.ToJson().HasColumnType(JsonColumnType));
    }

    public class MyEntityArrayOfPrimitives
    {
        public int Id { get; set; }
        public MyJsonEntityArrayOfPrimitives Reference { get; set; }
        public List<MyJsonEntityArrayOfPrimitives> Collection { get; set; }
    }

    public class MyJsonEntityArrayOfPrimitives
    {
        public int[] IntArray { get; set; }
        public List<string> ListOfString { get; set; }
    }

    #endregion

    #region JunkInJson

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Junk_in_json_basic_tracking(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelJunkInJson,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedJunkInJson);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityJunkInJson>();

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(2, result[0].Collection.Count);
            Assert.Equal(2, result[0].CollectionWithCtor.Count);
            Assert.Equal(2, result[0].Reference.NestedCollection.Count);
            Assert.NotNull(result[0].Reference.NestedReference);
            Assert.Equal(2, result[0].ReferenceWithCtor.NestedCollection.Count);
            Assert.NotNull(result[0].ReferenceWithCtor.NestedReference);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Junk_in_json_basic_no_tracking(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelJunkInJson,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedJunkInJson);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityJunkInJson>().AsNoTracking();

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(2, result[0].Collection.Count);
            Assert.Equal(2, result[0].CollectionWithCtor.Count);
            Assert.Equal(2, result[0].Reference.NestedCollection.Count);
            Assert.NotNull(result[0].Reference.NestedReference);
            Assert.Equal(2, result[0].ReferenceWithCtor.NestedCollection.Count);
            Assert.NotNull(result[0].ReferenceWithCtor.NestedReference);
        }
    }

    protected abstract Task SeedJunkInJson(DbContext ctx);

    protected virtual void BuildModelJunkInJson(ModelBuilder modelBuilder)
        => modelBuilder.Entity<MyEntityJunkInJson>(
            b =>
            {
                b.ToTable("Entities");
                b.Property(x => x.Id).ValueGeneratedNever();

                b.OwnsOne(
                    x => x.Reference, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.OwnsOne(x => x.NestedReference);
                        b.OwnsMany(x => x.NestedCollection);
                    });

                b.OwnsOne(
                    x => x.ReferenceWithCtor, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.OwnsOne(x => x.NestedReference);
                        b.OwnsMany(x => x.NestedCollection);
                    });

                b.OwnsMany(
                    x => x.Collection, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.OwnsOne(x => x.NestedReference);
                        b.OwnsMany(x => x.NestedCollection);
                    });

                b.OwnsMany(
                    x => x.CollectionWithCtor, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.OwnsOne(x => x.NestedReference);
                        b.OwnsMany(x => x.NestedCollection);
                    });
            });

    public class MyEntityJunkInJson
    {
        public int Id { get; set; }
        public MyJsonEntityJunkInJson Reference { get; set; }
        public MyJsonEntityJunkInJsonWithCtor ReferenceWithCtor { get; set; }
        public List<MyJsonEntityJunkInJson> Collection { get; set; }
        public List<MyJsonEntityJunkInJsonWithCtor> CollectionWithCtor { get; set; }
    }

    public class MyJsonEntityJunkInJson
    {
        public string Name { get; set; }
        public double Number { get; set; }

        public MyJsonEntityJunkInJsonNested NestedReference { get; set; }
        public List<MyJsonEntityJunkInJsonNested> NestedCollection { get; set; }
    }

    public class MyJsonEntityJunkInJsonNested
    {
        public DateTime DoB { get; set; }
    }

    public class MyJsonEntityJunkInJsonWithCtor(bool myBool, string name)
    {
        public bool MyBool { get; set; } = myBool;
        public string Name { get; set; } = name;

        public MyJsonEntityJunkInJsonWithCtorNested NestedReference { get; set; }
        public List<MyJsonEntityJunkInJsonWithCtorNested> NestedCollection { get; set; }
    }

    public class MyJsonEntityJunkInJsonWithCtorNested(DateTime doB)
    {
        public DateTime DoB { get; set; } = doB;
    }

    #endregion

    #region TrickyBuffering

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Tricky_buffering_basic(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelTrickyBuffering,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedTrickyBuffering);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityTrickyBuffering>();

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal("r1", result[0].Reference.Name);
            Assert.Equal(7, result[0].Reference.Number);
            Assert.Equal(new DateTime(2000, 1, 1), result[0].Reference.NestedReference.DoB);
            Assert.Equal(2, result[0].Reference.NestedCollection.Count);
        }
    }

    protected abstract Task SeedTrickyBuffering(DbContext ctx);

    protected virtual void BuildModelTrickyBuffering(ModelBuilder modelBuilder)
        => modelBuilder.Entity<MyEntityTrickyBuffering>(
            b =>
            {
                b.ToTable("Entities");
                b.Property(x => x.Id).ValueGeneratedNever();
                b.OwnsOne(
                    x => x.Reference, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.OwnsOne(x => x.NestedReference);
                        b.OwnsMany(x => x.NestedCollection);
                    });
            });

    public class MyEntityTrickyBuffering
    {
        public int Id { get; set; }
        public MyJsonEntityTrickyBuffering Reference { get; set; }
    }

    public class MyJsonEntityTrickyBuffering
    {
        public string Name { get; set; }
        public int Number { get; set; }
        public MyJsonEntityJunkInJsonNested NestedReference { get; set; }
        public List<MyJsonEntityJunkInJsonNested> NestedCollection { get; set; }
    }

    public class MyJsonEntityTrickyBufferingNested
    {
        public DateTime DoB { get; set; }
    }

    #endregion

    #region ShadowProperties

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Shadow_properties_basic_tracking(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelShadowProperties,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedShadowProperties);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityShadowProperties>();

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(2, result[0].Collection.Count);
            Assert.Equal(2, result[0].CollectionWithCtor.Count);
            Assert.NotNull(result[0].Reference);
            Assert.NotNull(result[0].ReferenceWithCtor);

            var referenceEntry = context.ChangeTracker.Entries().Single(x => x.Entity == result[0].Reference);
            Assert.Equal("Foo", referenceEntry.Property("ShadowString").CurrentValue);

            var referenceCtorEntry = context.ChangeTracker.Entries().Single(x => x.Entity == result[0].ReferenceWithCtor);
            Assert.Equal(143, referenceCtorEntry.Property("Shadow_Int").CurrentValue);

            var collectionEntry1 = context.ChangeTracker.Entries().Single(x => x.Entity == result[0].Collection[0]);
            var collectionEntry2 = context.ChangeTracker.Entries().Single(x => x.Entity == result[0].Collection[1]);
            Assert.Equal(5.5, collectionEntry1.Property("ShadowDouble").CurrentValue);
            Assert.Equal(20.5, collectionEntry2.Property("ShadowDouble").CurrentValue);

            var collectionCtorEntry1 = context.ChangeTracker.Entries().Single(x => x.Entity == result[0].CollectionWithCtor[0]);
            var collectionCtorEntry2 = context.ChangeTracker.Entries().Single(x => x.Entity == result[0].CollectionWithCtor[1]);
            Assert.Equal((byte)6, collectionCtorEntry1.Property("ShadowNullableByte").CurrentValue);
            Assert.Null(collectionCtorEntry2.Property("ShadowNullableByte").CurrentValue);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Shadow_properties_basic_no_tracking(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelShadowProperties,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedShadowProperties);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityShadowProperties>().AsNoTracking();

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(2, result[0].Collection.Count);
            Assert.Equal(2, result[0].CollectionWithCtor.Count);
            Assert.NotNull(result[0].Reference);
            Assert.NotNull(result[0].ReferenceWithCtor);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Project_shadow_properties_from_json_entity(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelShadowProperties,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedShadowProperties);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityShadowProperties>().Select(
                x => new
                {
                    ShadowString = EF.Property<string>(x.Reference, "ShadowString"),
                    ShadowInt = EF.Property<int>(x.ReferenceWithCtor, "Shadow_Int"),
                });

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal("Foo", result[0].ShadowString);
            Assert.Equal(143, result[0].ShadowInt);
        }
    }

    protected abstract Task SeedShadowProperties(DbContext ctx);

    protected virtual void BuildModelShadowProperties(ModelBuilder modelBuilder)
        => modelBuilder.Entity<MyEntityShadowProperties>(
            b =>
            {
                b.ToTable("Entities");
                b.Property(x => x.Id).ValueGeneratedNever();

                b.OwnsOne(
                    x => x.Reference, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.Property<string>("ShadowString");
                    });

                b.OwnsOne(
                    x => x.ReferenceWithCtor, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.Property<int>("Shadow_Int").HasJsonPropertyName("ShadowInt");
                    });

                b.OwnsMany(
                    x => x.Collection, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.Property<double>("ShadowDouble");
                    });

                b.OwnsMany(
                    x => x.CollectionWithCtor, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.Property<byte?>("ShadowNullableByte");
                    });
            });

    public class MyEntityShadowProperties
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public MyJsonEntityShadowProperties Reference { get; set; }
        public List<MyJsonEntityShadowProperties> Collection { get; set; }
        public MyJsonEntityShadowPropertiesWithCtor ReferenceWithCtor { get; set; }
        public List<MyJsonEntityShadowPropertiesWithCtor> CollectionWithCtor { get; set; }
    }

    public class MyJsonEntityShadowProperties
    {
        public string Name { get; set; }
    }

    public class MyJsonEntityShadowPropertiesWithCtor(string name)
    {
        public string Name { get; set; } = name;
    }

    #endregion

    #region LazyLoadingProxies

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Project_proxies_entity_with_json(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelLazyLoadingProxies,
            seed: SeedLazyLoadingProxies,
            onConfiguring: b =>
            {
                b = b.ConfigureWarnings(ConfigureWarnings);
                OnConfiguringLazyLoadingProxies(b);
            },
            addServices: AddServicesLazyLoadingProxies);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityLazyLoadingProxies>();

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(2, result.Count);
        }
    }

    protected void OnConfiguringLazyLoadingProxies(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseLazyLoadingProxies();

    protected IServiceCollection AddServicesLazyLoadingProxies(IServiceCollection addServices)
        => addServices.AddEntityFrameworkProxies();

    private Task SeedLazyLoadingProxies(DbContext ctx)
    {
        var r1 = new MyJsonEntityLazyLoadingProxiesWithCtor("r1", 1);
        var c11 = new MyJsonEntityLazyLoadingProxies { Name = "c11", Number = 11 };
        var c12 = new MyJsonEntityLazyLoadingProxies { Name = "c12", Number = 12 };
        var c13 = new MyJsonEntityLazyLoadingProxies { Name = "c13", Number = 13 };

        var r2 = new MyJsonEntityLazyLoadingProxiesWithCtor("r2", 2);
        var c21 = new MyJsonEntityLazyLoadingProxies { Name = "c21", Number = 21 };
        var c22 = new MyJsonEntityLazyLoadingProxies { Name = "c22", Number = 22 };

        var e1 = new MyEntityLazyLoadingProxies
        {
            Id = 1,
            Name = "e1",
            Reference = r1,
            Collection =
            [
                c11,
                c12,
                c13
            ]
        };

        var e2 = new MyEntityLazyLoadingProxies
        {
            Id = 2,
            Name = "e2",
            Reference = r2,
            Collection = [c21, c22]
        };

        ctx.Set<MyEntityLazyLoadingProxies>().AddRange(e1, e2);
        return ctx.SaveChangesAsync();
    }

    protected virtual void BuildModelLazyLoadingProxies(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MyEntityLazyLoadingProxies>().Property(x => x.Id).ValueGeneratedNever();
        modelBuilder.Entity<MyEntityLazyLoadingProxies>().OwnsOne(x => x.Reference, b => b.ToJson().HasColumnType(JsonColumnType));
        modelBuilder.Entity<MyEntityLazyLoadingProxies>().OwnsMany(x => x.Collection, b => b.ToJson().HasColumnType(JsonColumnType));
    }

    public class MyEntityLazyLoadingProxies
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual MyJsonEntityLazyLoadingProxiesWithCtor Reference { get; set; }
        public virtual List<MyJsonEntityLazyLoadingProxies> Collection { get; set; }
    }

    public class MyJsonEntityLazyLoadingProxiesWithCtor(string name, int number)
    {
        public string Name { get; set; } = name;
        public int Number { get; set; } = number;
    }

    public class MyJsonEntityLazyLoadingProxies
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }

    #endregion

    #region NotICollection

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Not_ICollection_basic_projection(bool async)
    {
        var contextFactory = await InitializeAsync<DbContext>(
            onModelCreating: BuildModelNotICollection,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedNotICollection);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyEntityNotICollection>();

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(2, result.Count);
        }
    }

    protected abstract Task SeedNotICollection(DbContext ctx);

    public class MyEntityNotICollection
    {
        public int Id { get; set; }

        public MyJsonEntityNotICollection Json { get; set; }
    }

    public class MyJsonEntityNotICollection
    {
        private readonly List<MyJsonNestedEntityNotICollection> _collection = [];

        public IEnumerable<MyJsonNestedEntityNotICollection> Collection
            => _collection.AsReadOnly();
    }

    public class MyJsonNestedEntityNotICollection
    {
        public string Foo { get; set; }
        public int Bar { get; set; }
    }

    protected virtual void BuildModelNotICollection(ModelBuilder modelBuilder)
        => modelBuilder.Entity<MyEntityNotICollection>(
            b =>
            {
                b.ToTable("Entities");
                b.Property(x => x.Id).ValueGeneratedNever();
                b.OwnsOne(
                    cr => cr.Json, nb =>
                    {
                        nb.ToJson().HasColumnType(JsonColumnType);
                        nb.OwnsMany(x => x.Collection);
                    });
            });

    #endregion

    #region BadJsonProperties

    [ConditionalFact]
    public virtual async Task Bad_json_properties_duplicated_navigations_tracking()
    {
        var contextFactory = await InitializeAsync<ContextBadJsonProperties>(
            onModelCreating: BuildModelBadJsonProperties,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedBadJsonProperties);

        using (var context = contextFactory.CreateContext())
        {
            var baseline = await context.Entities.SingleAsync(x => x.Scenario == "baseline");
            var dupNavs = await context.Entities.SingleAsync(x => x.Scenario == "duplicated navigations");

            // for tracking, first one wins
            Assert.Equal(baseline.RequiredReference.NestedOptional.Text, dupNavs.RequiredReference.NestedOptional.Text);
            Assert.Equal(baseline.RequiredReference.NestedRequired.Text, dupNavs.RequiredReference.NestedRequired.Text);
            Assert.Equal(baseline.RequiredReference.NestedCollection[0].Text, dupNavs.RequiredReference.NestedCollection[0].Text);
            Assert.Equal(baseline.RequiredReference.NestedCollection[1].Text, dupNavs.RequiredReference.NestedCollection[1].Text);

            Assert.Equal(baseline.OptionalReference.NestedOptional.Text, dupNavs.OptionalReference.NestedOptional.Text);
            Assert.Equal(baseline.OptionalReference.NestedRequired.Text, dupNavs.OptionalReference.NestedRequired.Text);
            Assert.Equal(baseline.OptionalReference.NestedCollection[0].Text, dupNavs.OptionalReference.NestedCollection[0].Text);
            Assert.Equal(baseline.OptionalReference.NestedCollection[1].Text, dupNavs.OptionalReference.NestedCollection[1].Text);

            Assert.Equal(baseline.Collection[0].NestedOptional.Text, dupNavs.Collection[0].NestedOptional.Text);
            Assert.Equal(baseline.Collection[0].NestedRequired.Text, dupNavs.Collection[0].NestedRequired.Text);
            Assert.Equal(baseline.Collection[0].NestedCollection[0].Text, dupNavs.Collection[0].NestedCollection[0].Text);
            Assert.Equal(baseline.Collection[0].NestedCollection[1].Text, dupNavs.Collection[0].NestedCollection[1].Text);

            Assert.Equal(baseline.Collection[1].NestedOptional.Text, dupNavs.Collection[1].NestedOptional.Text);
            Assert.Equal(baseline.Collection[1].NestedRequired.Text, dupNavs.Collection[1].NestedRequired.Text);
            Assert.Equal(baseline.Collection[1].NestedCollection[0].Text, dupNavs.Collection[1].NestedCollection[0].Text);
            Assert.Equal(baseline.Collection[1].NestedCollection[1].Text, dupNavs.Collection[1].NestedCollection[1].Text);
        }
    }

    [ConditionalFact]
    public virtual async Task Bad_json_properties_duplicated_navigations_no_tracking()
    {
        var contextFactory = await InitializeAsync<ContextBadJsonProperties>(
            onModelCreating: BuildModelBadJsonProperties,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedBadJsonProperties);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.AsNoTracking();

            var baseline = query.Single(x => x.Scenario == "baseline");
            var dupNavs = query.Single(x => x.Scenario == "duplicated navigations");

            // for no tracking, last one wins
            Assert.Equal(baseline.RequiredReference.NestedOptional.Text + " dupnav", dupNavs.RequiredReference.NestedOptional.Text);
            Assert.Equal(baseline.RequiredReference.NestedRequired.Text + " dupnav", dupNavs.RequiredReference.NestedRequired.Text);
            Assert.Equal(baseline.RequiredReference.NestedCollection[0].Text + " dupnav", dupNavs.RequiredReference.NestedCollection[0].Text);
            Assert.Equal(baseline.RequiredReference.NestedCollection[1].Text + " dupnav", dupNavs.RequiredReference.NestedCollection[1].Text);

            Assert.Equal(baseline.OptionalReference.NestedOptional.Text + " dupnav", dupNavs.OptionalReference.NestedOptional.Text);
            Assert.Equal(baseline.OptionalReference.NestedRequired.Text + " dupnav", dupNavs.OptionalReference.NestedRequired.Text);
            Assert.Equal(baseline.OptionalReference.NestedCollection[0].Text + " dupnav", dupNavs.OptionalReference.NestedCollection[0].Text);
            Assert.Equal(baseline.OptionalReference.NestedCollection[1].Text + " dupnav", dupNavs.OptionalReference.NestedCollection[1].Text);

            Assert.Equal(baseline.Collection[0].NestedOptional.Text + " dupnav", dupNavs.Collection[0].NestedOptional.Text);
            Assert.Equal(baseline.Collection[0].NestedRequired.Text + " dupnav", dupNavs.Collection[0].NestedRequired.Text);
            Assert.Equal(baseline.Collection[0].NestedCollection[0].Text + " dupnav", dupNavs.Collection[0].NestedCollection[0].Text);
            Assert.Equal(baseline.Collection[0].NestedCollection[1].Text + " dupnav", dupNavs.Collection[0].NestedCollection[1].Text);

            Assert.Equal(baseline.Collection[1].NestedOptional.Text + " dupnav", dupNavs.Collection[1].NestedOptional.Text);
            Assert.Equal(baseline.Collection[1].NestedRequired.Text + " dupnav", dupNavs.Collection[1].NestedRequired.Text);
            Assert.Equal(baseline.Collection[1].NestedCollection[0].Text + " dupnav", dupNavs.Collection[1].NestedCollection[0].Text);
            Assert.Equal(baseline.Collection[1].NestedCollection[1].Text + " dupnav", dupNavs.Collection[1].NestedCollection[1].Text);
        }
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public virtual async Task Bad_json_properties_duplicated_scalars(bool noTracking)
    {
        var contextFactory = await InitializeAsync<ContextBadJsonProperties>(
            onModelCreating: BuildModelBadJsonProperties,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedBadJsonProperties);

        using (var context = contextFactory.CreateContext())
        {
            var query = noTracking ? context.Entities.AsNoTracking() : context.Entities;

            var baseline = await query.SingleAsync(x => x.Scenario == "baseline");
            var dupProps = await query.SingleAsync(x => x.Scenario == "duplicated scalars");

            Assert.Equal(baseline.RequiredReference.NestedOptional.Text + " dupprop", dupProps.RequiredReference.NestedOptional.Text);
            Assert.Equal(baseline.RequiredReference.NestedRequired.Text + " dupprop", dupProps.RequiredReference.NestedRequired.Text);
            Assert.Equal(baseline.RequiredReference.NestedCollection[0].Text + " dupprop", dupProps.RequiredReference.NestedCollection[0].Text);
            Assert.Equal(baseline.RequiredReference.NestedCollection[1].Text + " dupprop", dupProps.RequiredReference.NestedCollection[1].Text);

            Assert.Equal(baseline.OptionalReference.NestedOptional.Text + " dupprop", dupProps.OptionalReference.NestedOptional.Text);
            Assert.Equal(baseline.OptionalReference.NestedRequired.Text + " dupprop", dupProps.OptionalReference.NestedRequired.Text);
            Assert.Equal(baseline.OptionalReference.NestedCollection[0].Text + " dupprop", dupProps.OptionalReference.NestedCollection[0].Text);
            Assert.Equal(baseline.OptionalReference.NestedCollection[1].Text + " dupprop", dupProps.OptionalReference.NestedCollection[1].Text);

            Assert.Equal(baseline.Collection[0].NestedOptional.Text + " dupprop", dupProps.Collection[0].NestedOptional.Text);
            Assert.Equal(baseline.Collection[0].NestedRequired.Text + " dupprop", dupProps.Collection[0].NestedRequired.Text);
            Assert.Equal(baseline.Collection[0].NestedCollection[0].Text + " dupprop", dupProps.Collection[0].NestedCollection[0].Text);
            Assert.Equal(baseline.Collection[0].NestedCollection[1].Text + " dupprop", dupProps.Collection[0].NestedCollection[1].Text);

            Assert.Equal(baseline.Collection[1].NestedOptional.Text + " dupprop", dupProps.Collection[1].NestedOptional.Text);
            Assert.Equal(baseline.Collection[1].NestedRequired.Text + " dupprop", dupProps.Collection[1].NestedRequired.Text);
            Assert.Equal(baseline.Collection[1].NestedCollection[0].Text + " dupprop", dupProps.Collection[1].NestedCollection[0].Text);
            Assert.Equal(baseline.Collection[1].NestedCollection[1].Text + " dupprop", dupProps.Collection[1].NestedCollection[1].Text);
        }
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public virtual async Task Bad_json_properties_empty_navigations(bool noTracking)
    {
        var contextFactory = await InitializeAsync<ContextBadJsonProperties>(
            onModelCreating: BuildModelBadJsonProperties,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedBadJsonProperties);

        using (var context = contextFactory.CreateContext())
        {
            var query = noTracking ? context.Entities.AsNoTracking() : context.Entities;
            var emptyNavs = await query.SingleAsync(x => x.Scenario == "empty navigation property names");

            Assert.Null(emptyNavs.RequiredReference.NestedOptional);
            Assert.Null(emptyNavs.RequiredReference.NestedRequired);
            Assert.Null(emptyNavs.RequiredReference.NestedCollection);

            Assert.Null(emptyNavs.OptionalReference.NestedOptional);
            Assert.Null(emptyNavs.OptionalReference.NestedRequired);
            Assert.Null(emptyNavs.OptionalReference.NestedCollection);

            Assert.Null(emptyNavs.Collection[0].NestedOptional);
            Assert.Null(emptyNavs.Collection[0].NestedRequired);
            Assert.Null(emptyNavs.Collection[0].NestedCollection);

            Assert.Null(emptyNavs.Collection[1].NestedOptional);
            Assert.Null(emptyNavs.Collection[1].NestedRequired);
            Assert.Null(emptyNavs.Collection[1].NestedCollection);
        }
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public virtual async Task Bad_json_properties_empty_scalars(bool noTracking)
    {
        var contextFactory = await InitializeAsync<ContextBadJsonProperties>(
            onModelCreating: BuildModelBadJsonProperties,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedBadJsonProperties);

        using (var context = contextFactory.CreateContext())
        {
            var query = noTracking ? context.Entities.AsNoTracking() : context.Entities;
            var emptyNavs = await query.SingleAsync(x => x.Scenario == "empty scalar property names");

            Assert.Null(emptyNavs.RequiredReference.NestedOptional.Text);
            Assert.Null(emptyNavs.RequiredReference.NestedRequired.Text);
            Assert.Null(emptyNavs.RequiredReference.NestedCollection[0].Text);
            Assert.Null(emptyNavs.RequiredReference.NestedCollection[1].Text);

            Assert.Null(emptyNavs.OptionalReference.NestedOptional.Text);
            Assert.Null(emptyNavs.OptionalReference.NestedRequired.Text);
            Assert.Null(emptyNavs.OptionalReference.NestedCollection[0].Text);
            Assert.Null(emptyNavs.OptionalReference.NestedCollection[1].Text);

            Assert.Null(emptyNavs.Collection[0].NestedOptional.Text);
            Assert.Null(emptyNavs.Collection[0].NestedRequired.Text);
            Assert.Null(emptyNavs.Collection[0].NestedCollection[0].Text);
            Assert.Null(emptyNavs.Collection[0].NestedCollection[1].Text);

            Assert.Null(emptyNavs.Collection[1].NestedOptional.Text);
            Assert.Null(emptyNavs.Collection[1].NestedRequired.Text);
            Assert.Null(emptyNavs.Collection[1].NestedCollection[0].Text);
            Assert.Null(emptyNavs.Collection[1].NestedCollection[1].Text);
        }
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public virtual async Task Bad_json_properties_null_navigations(bool noTracking)
    {
        var contextFactory = await InitializeAsync<ContextBadJsonProperties>(
            onModelCreating: BuildModelBadJsonProperties,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedBadJsonProperties);

        using (var context = contextFactory.CreateContext())
        {
            var query = noTracking ? context.Entities.AsNoTracking() : context.Entities;

            await Assert.ThrowsAnyAsync<JsonException>(
                () => query.SingleAsync(x => x.Scenario == "null navigation property names"));
        }
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public virtual async Task Bad_json_properties_null_scalars(bool noTracking)
    {
        var contextFactory = await InitializeAsync<ContextBadJsonProperties>(
            onModelCreating: BuildModelBadJsonProperties,
            onConfiguring: b => b.ConfigureWarnings(ConfigureWarnings),
            seed: SeedBadJsonProperties);

        using (var context = contextFactory.CreateContext())
        {
            var query = noTracking ? context.Entities.AsNoTracking() : context.Entities;

            var message = (await Assert.ThrowsAnyAsync<JsonException>(
                () => query.SingleAsync(x => x.Scenario == "null scalar property names"))).Message;

            Assert.StartsWith("'n' is an invalid start of a property name. Expected a '\"'.", message);
        }
    }

    protected class ContextBadJsonProperties(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Entity> Entities { get; set; }

        public class Entity
        {
            public int Id { get; set; }
            public string Scenario { get; set; }
            public JsonRoot OptionalReference { get; set; }
            public JsonRoot RequiredReference { get; set; }
            public List<JsonRoot> Collection { get; set; }
        }

        public class JsonRoot
        {
            public JsonBranch NestedRequired { get; set; }
            public JsonBranch NestedOptional { get; set; }
            public List<JsonBranch> NestedCollection { get; set; }
        }

        public class JsonBranch
        {
            public string Text { get; set; }
        }
    }

    protected abstract Task SeedBadJsonProperties(ContextBadJsonProperties ctx);

    protected virtual void BuildModelBadJsonProperties(ModelBuilder modelBuilder)
        => modelBuilder.Entity<ContextBadJsonProperties.Entity>(
            b =>
            {
                b.ToTable("Entities");
                b.Property(x => x.Id).ValueGeneratedNever();

                b.OwnsOne(
                    x => x.RequiredReference, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.OwnsOne(x => x.NestedOptional);
                        b.OwnsOne(x => x.NestedRequired);
                        b.OwnsMany(x => x.NestedCollection);
                    });

                b.OwnsOne(
                    x => x.OptionalReference, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.OwnsOne(x => x.NestedOptional);
                        b.OwnsOne(x => x.NestedRequired);
                        b.OwnsMany(x => x.NestedCollection);
                    });

                b.OwnsMany(
                    x => x.Collection, b =>
                    {
                        b.ToJson().HasColumnType(JsonColumnType);
                        b.OwnsOne(x => x.NestedOptional);
                        b.OwnsOne(x => x.NestedRequired);
                        b.OwnsMany(x => x.NestedCollection);
                    });
            });

    #endregion

    protected TestSqlLoggerFactory TestSqlLoggerFactory
        => (TestSqlLoggerFactory)ListLoggerFactory;

    protected virtual string JsonColumnType
        => null;
}
