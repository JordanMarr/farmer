module Cosmos

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Storage
open Microsoft.Azure.Management.Storage
open Microsoft.Azure.Management.Storage.Models
open Microsoft.Rest
open System

let tests = testList "Cosmos" [
    test "Cosmos container should ignore duplicate unique keys" {

        let container =
            cosmosContainer {
                name "people"
                partition_key [ "/id" ] CosmosDb.Hash
                add_unique_key [ "/FirstName" ]
                add_unique_key [ "/LastName" ]
                add_unique_key [ "/LastName" ]
            }

        Expect.equal container.UniqueKeys.Count 2 "There should be 2 unique keys."
        Expect.contains container.UniqueKeys ["/FirstName"] "UniqueKeys should contain /FirstName"
        Expect.contains container.UniqueKeys ["/LastName"] "UniqueKeys should contain /LastName"
    }
    test "Serverless template should include 'EnableServerless' and exclude 'throughput'" {
        let t = arm { add_resource (cosmosDb { name "foo"; serverless; }) }
        let json = t.Template |> Writer.toJson
        Expect.isTrue (json.Contains("EnableServerless")) "Serverless template should contain 'EnableServerless'."
        Expect.isFalse (json.Contains("throughput")) "Serverless template should not contain 'throughput'."
    }
    test "Shared throughput should include 'throughput' and exclude 'EnableServerless'" {
        let t = arm { add_resource (cosmosDb { name "foo"; throughput 400<CosmosDb.RU>; }) }
        let json = t.Template |> Writer.toJson
        Expect.isTrue (json.Contains("\"throughput\": \"400\"")) "Shared throughput template should contain 'throughput'."
        Expect.isFalse (json.Contains("EnableServerless")) "Shared throughput template should not contain 'EnableServerless'."
    }
    test "DB properties are correctly evaluated" {
        let db = cosmosDb { name "test" }
        Expect.equal "[reference(concat('Microsoft.DocumentDb/databaseAccounts/', 'test-account')).documentEndpoint]" (db.Endpoint.Eval()) "Endpoint is incorrect"
        Expect.equal "[listKeys(resourceId('Microsoft.DocumentDB/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDB','databaseAccounts').apiVersions[0]).primaryMasterKey]" (db.PrimaryKey.Eval()) "Primary Key is incorrect"
        Expect.equal "[listKeys(resourceId('Microsoft.DocumentDB/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDB','databaseAccounts').apiVersions[0]).secondaryMasterKey]" (db.SecondaryKey.Eval()) "Secondary Key is incorrect"
        Expect.equal "[listKeys(resourceId('Microsoft.DocumentDB/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDB','databaseAccounts').apiVersions[0]).primaryReadonlyMasterKey]" (db.PrimaryReadonlyKey.Eval()) "Primary Readonly Key is incorrect"
        Expect.equal "[listKeys(resourceId('Microsoft.DocumentDB/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDB','databaseAccounts').apiVersions[0]).secondaryReadonlyMasterKey]" (db.SecondaryReadonlyKey.Eval()) "Secondary Readonly Key is incorrect"
        Expect.equal "[listConnectionStrings(resourceId('Microsoft.DocumentDB/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDB','databaseAccounts').apiVersions[0]).connectionStrings[0].connectionString]" (db.PrimaryConnectionString.Eval()) "Primary Connection String is incorrect"
        Expect.equal "[listConnectionStrings(resourceId('Microsoft.DocumentDB/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDB','databaseAccounts').apiVersions[0]).connectionStrings[1].connectionString]" (db.SecondaryConnectionString.Eval()) "Secondary Connection String is incorrect"
    }
    test "Correctly serializes to JSON" {
        let t = arm {
            add_resource (cosmosDb { name "test" })
        }

        t.Template
        |> Writer.toJson
        |> ignore
    }
]
