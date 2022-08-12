# TrickEngineUnity
Utility Framework for developing games in Unity

### Table of contents
1. [TrickCore](#trickcore)
2. [TrickAddressables](#trickaddressables)
3. [TrickDropTable](#trickdroptable)
4. [TrickGame](#trickgame)
5. [TrickREST](#trickrest)
6. [TrickSQL / TrickMySQL](#tricksql--trickmysql)
7. [TrickTime](#tricktime)

## TrickCore
Required for all other modules

## TrickAddressables

## TrickDropTable

#### What is a DropTable
Equivalent to a Loot Table which is a set of items is put into a ``DropTable<T>``. This is just a bucket of items with a weight. 
An example on how you can use it. You have a list of Swords, some swords are Extremely rare those have a `low weight`, some other swords are less rare, so you give it a `higher weight`. 

All items in the DropTable are summed up and normalized to 1. And a randomizer rolls from 0-1 and this defines which item you've rolled.

````csharp
// Creates a new droptable with 3 items in it 
DropTable<int> dropTable = new DropTable<int>()
{
    {1,10}, // Number 1 with a weight of 10 
    {2,30}, // Number 2 with a weight of 30  
    {3,60}, // Number 3 with a weight of 60
};

// Add Number 100 with a weight of 100
dropTable.Add(100, 100);

// Get the normalized weights of all entries
// [1, 0.0500000007450581],
// [2, 0.150000005960464],
// [3, 0.300000011920929],
// [100, 0.5]
dropTable.GetAllNormalizedWeights();

// removes a value from the droptable, returns true if successfully removed
dropTable.Remove(100);

// sets the weight of a value, returns true if the weight is set
dropTable.SetObjectWeight(3,160);

/// Ways to roll a droptable using different Randomizers.

// Roll the DropTable using SeedRandom (Wrapped System.Random)
SeedRandom.Default.RandomItem(dropTable);
// Roll the DropTable using StrongRandom (Wrapped System.Security.Cryptography.RandomNumberGenerator)
StrongRandom.Default.RandomItem(dropTable);
// Roll the DropTable using PcgRxsMXs64
TrickIRandomizer.DefaultPcg32.RandomItem(dropTable);

// Roll the droptable 2 times, and returns 2 items from it. Allow pulling duplicates
SeedRandom.Default.RandomItems(dropTable, 2, true);
// Roll the droptable 3 times, and returns 3 items from it. Don't allow pulling duplicates
SeedRandom.Default.RandomItems(dropTable, 2, false);
````

## TrickGame

UIManager
UIMenu

## TrickREST

RESTGet<T>
RESTPost<T>
RESTDelete<T>

## TrickSQL / TrickMySQL



## TrickTime

TrickTime.CurrentServerTime

## TrickRandomizer

SeedRandom.Default
StrongRandom.Default
TrickIRandomizer.DefaultPcg32