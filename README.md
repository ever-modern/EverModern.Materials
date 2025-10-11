# DestallPackages

Different packages serving general purposes.

------------------------------------------------------------------------------------------------------------------

WheelProtection.* - packages to be imported in order not to write common utility code from scratch again and again.

------------------------------------------------------------------------------------------------------------------

  Queues - Contains two classes.  
    RateController: allows to limit some actions according within prescribed time ranges. May be used to comply with outer API service requests-per-second (or -per-minute/hour or any arbitraty period of time) constraints. Implement abstract class and follow the annotations to activate it.     
    Recycler: works as a generic type pool with asynchronous items retrieval and ability to return the item back to it. May be used to setup connection pools, that don't throw if the limit is reached. Substitute for standard ObjectPool. Implement abstract class and follow the annotations to activate it. 

------------------------------------------------------------------------------------------------------------------

CodeGeneration - most ambitious of all in the repo. Allows to get compilation object of targetted project and bind it to blazor component, that will generate code, according to its content, formed as if it was a Razor page.
Just like that:
```
<File Path="TargetProject/Auto.cs">

  public static class Demo
  {
    @for (int i = 0; i < 10; i++)
    {
      <text>
        Console.WriteLine($"{i}. Hello, World!");    
      </text>
    }
    
  }

</File>
```
Please, see the example and readme.md at samples/*.sln for closer introduction and template files structure to start off with. Comments in source files will guide you.

***
Requires some knowledge of Roslyn. 
Thorough course on Roslyn can be found at 
https://joshvarty.com/learn-roslyn-now/ - still worthy  
Much thanks to [Josh Varty](https://github.com/JoshVarty) 
***

I owe a great deal of gratitude to the 
[BlazorTemplater](https://github.com/conficient/BlazorTemplater)  
and  
[Buildalyzer](https://github.com/daveaglick/Buildalyzer)  
projects, as mine is effectively a bundle of those two invaluable ones.
  
