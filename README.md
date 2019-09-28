# unit-test-generator

## What does unit-test-generator do?

What does every developer hate? Yeah... writing tests. Tests are most of the times simple copy and paste. We need to do many things like setting up Mocks for injected dependencies, Add tests for all public methods and setup mocks.

This is where unit-test-generator comes into play! It takes a class and generates simple tests and mocks the calls the the methods.

The following things are currently supported:

- Files
  - TestFile is directly generated in your test project `MyProject.Test`
    example: `MyProject/Services/HelloService.cs` generates `MyProject.Test/Services/HelloServiceTest.cs`
- Mocks
  - Create private Mock instances from construtor injected dependencies and give them meaningfull names (IBigService => \_bigService)
- Test Methods
  - Generate TestMethods for all public methods in the following form ShouldTestBigIntMethod
  - Setup methods on mocks that are called in method
  - Execute the method that should be tested
  - Call VerifyAll at the end of all mocks
- Usings
  - Add usings to Moq, FluentAssertions and XUnit
  - Add using to class that is tested

## How to Install/Update/Uninstall unit-test-generator

### Install

1. Download the nuget package from releases
2. `dotnet tool install Opiskull.UnitTestGenerator --add-source ./download-folder -g`

### Update

1. Download the nuget package from releases
2. `dotnet tool update Opiskull.UnitTestGenerator --add-source ./download-folder -g`

### Uninstall

1. `dotnet tool uninstall Opiskull.UnitTestGenerator -g`

## How to use unit-test-generator

You can start the tool with `unit-test-generator ./InputClassFile.cs`.

After you have executed the tool you can read the generated file and copy it to your tests folder.

With `unit-test-generator ./MyProject/Services/HelloService.cs` you can generate the file directly in your test project.

## The following commands are supported

```
-v, --verbose Set output to verbose messages.

-s, --skip Skip creation of output file.

-o, --overwrite Overwrite output file.

--help Display this help screen.

--version Display version information.

value pos. 0 Required. Input filename.

value pos. 1 Output filename.
```
