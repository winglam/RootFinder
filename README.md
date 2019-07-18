This repository contains the RootFinder tool and dataset of anonymized execution logs of flaky tests for the "[Root Causing Flaky Tests in a Large-Scale Industrial Setting](http://winglam2.web.engr.illinois.edu/publications/2019/LamETAL19RootFinder.pdf)" paper.

For any questions regarding the paper, tool, or dataset, please feel free to reach out to [winglam2@illinois.edu](mailto:winglam2@illinois.edu). 

# RootFinder

This part of the README explains how the tool to automatically parse the logs to root cause flaky tests. Please note the following:
+ The instructions below are meant to be executed within a Windows Powershell. Using another command-line shell may require changes to the commands below.
+ The format in which the logs are expected to be is explained [here](https://github.com/winglam/RootFinder/tree/master/AnonymizedLogs).

## Root causing without domain knowledge
```
.\LogParser.exe `
  -passingLogsDir *dir_containing_passing_logs* `
  -failingLogsDir *dir_containing_failing_logs* `
  -type Relative `
  -outputDir .\
```
+ ```-passingLogsDir``` the directory that contains the passing logs for your subject.
+ ```-failingLogsDir``` the directory that contains the failing logs for your subject.
+ ```-type``` predicate type. Possible types are Relative, Absolute, Exception, Slow, and Fast.
+ ```-outputDir``` the directory to output the information found by RootFinder.


## Root causing with doman knowledge
```
.\RootFinder.exe `
  -passingLogsDir *dir_containing_passing_logs* `
  -failingLogsDir *dir_containing_failing_logs* `
  -type Relative `
  -outputDir .\ `
  -methodName "System.Random.Next"
```

+ ```-passingLogsDir``` the directory that contains the passing logs for your subject.
+ ```-failingLogsDir``` the directory that contains the failing logs for your subject.
+ ```-type``` predicate type. Possible types are Relative, Absolute, Exception, Slow, and Fast.
+ ```-outputDir``` the directory to output the information found by RootFinder.
+ ```-methodName``` fully qualified method name that is likely to be the cause of the flakiness.
+ ```-predicate_Val``` optional parameter for predicates (e.g., Absolute, Exception, Slow, Fast) that can use an additional parameter.

## Output
This tool should then output the following files in ```.\```.
+ ```Relative.xml``` XML file highlighting the lines in the passing and failing logs whose return values did or did not satisfy the Relative predicate. (If another predicate is used then the name of file is of that predicate instead).
+ ```predicateFiles\``` Directory containing predicate files for each passing or failing log in XML format.
+ ```PredicatesPerFile.csv``` CSV file where each line represents the return value(s) of ```-methodName``` in a passing/failing execution of the flaky test.


# Anonymized Dataset

The anonymized dataset of 44 flaky unit tests is stored [here](https://github.com/winglam/RootFinder/tree/master/AnonymizedLogs). They belong to 22 software projects from 18 Microsoft internal/external products and services. For each test, the dataset contains 100 logs, some of which are for failed executions. 

We believe that the dataset will be useful to the research community, not only to conduct research on various aspects of flaky tests and their root causes, but also for a general understanding of runtime behavior of tests in a production system. In the anonymized dataset, sensitive strings (such as method names containing Microsoft product names) are replaced with their hash values; however, the hashes are deterministic and hence can be correlated within and across trace files.
