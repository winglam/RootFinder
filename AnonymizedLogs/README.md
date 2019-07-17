# Microsoft Flaky Test Dataset

The folder contains anonymized execution traces from a number of flaky tests run in Microsoft. The directory contains trace files of passing and failing runs, from various tests from various test binaries from various project. The directory has the following structure:
```
Project_1.zip/
+--Test_Binary_1/
¦  +--Test_1/
¦  ¦  +--Pass/
¦  ¦  ¦  +-- trace1 from passing test run
¦  ¦  ¦  +-- trace2 from passing test run
¦  ¦  ¦  +-- ...
¦  ¦  +--Fail/
¦  ¦  ¦  +-- trace1 from failing test run
¦  ¦  ¦  +-- trace2 from failing test run
¦  ¦  ¦  +-- ...
...
```
For anonymity, project, test binary, and test names have been replaced with numbers.     

### Basic statistics about the dataset
* Number of flaky tests: 44, number of projects: 22
* Avg. % of failed executions/ test: 28%
* Avg. # of method calls /test: 335K
* Avg. # of unique methods /test: 335
* Avg. # of threads/test: 5
* Avt. # of objects/test: 55K

### Parsing a tracefile
Each tracefile contains a header line, followed by a sequence of records. Each record is represented by a line and it contains various runtime information of an executed method. The records are sorted based on the start time of the corresponding method. 

Each record (or, line) contains the following fields about an executed method:

<table width="300">
<tr><td>(0)Ticks</td><td>Start time of the method, in ticks.</td></tr>
<tr><td>(1)Context</td><td>A runtime unique id of the method. Can be ignored for this dataset.</td></tr>
<tr><td>(2)ManagedThreadId</td><td>Id of the thread the method is executing on</td></tr>
<tr><td>(3)HitSequence</td><td>Number of times the method has been executed so far at the current location. If a method Foo at a specific locaton is called multiple times (e.g., because it's in a loop), the first call will have a HitSequence of 1, the second call will have 2, and so on.</td></tr>
<tr><td>(4)ObjectId</td><td>A unique ID of the object of the method. 0, if the method is static.</td></tr>
<tr><td>(5)ObjType</td><td>Type of the object.</td></tr> 
<tr><td>(6)MethodName</td><td>Signature of the parent method (the method calling the current method).</td></tr>
<tr><td>(7)ApiName</td><td>Signature of the method.</td></tr>
<tr><td>(8)ParentThreadId</td><td>Id of the thread that spawned the current thread. 0, if there is no parent thread.</td></tr>
<tr><td>(9)RequestId</td><td>A ID unique to the entire request. Can be ignored for this dataset.</td></tr>
<tr><td>(10)IlOffset</td><td>ILOffset of the method call. (6)MethodName+(10)IlOffset uniquely identify a method call's location in the code.</td></tr>
<tr><td>(11)LineNumber</td><td> Source line number of the method call. This information is available only if the program database file (PDB) is available during runtime; 0 otherwise.</td></tr>
<tr><td>(12)Latency(ticks)</td><td>Latency of the method, in ticks.</td></tr>
<tr><td>(13)ReturnValue</td><td>Return value of the method. If the method returns an object, ReturnValue contains the output of Object.ToString().</td></tr>
<tr><td>(14)Exception</td><td>Name of the exception the method has thrown. Empty if no exception was thrown.</td></tr>
</table>