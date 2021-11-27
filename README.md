# Async and await
keywords came with C# 5 as a cool new feature for handling asynchronous tasks. They allow us to specify tasks to be executed asynchronously in an easy and straightforward fashion. However, some people are mystified by asynchronous programming and are not sure how it actually works. I will try to give you an insight of the magic that happens under the hood when async and await are used.

# Awaiter Pattern
C# language compiles some of the features as a syntactic sugar, which means that certain language features are just conveniences that translate into existing language features. A lot of those syntactic sugar features expand into patterns. Those patterns are based on method calls, property lookups or interface implementations. await expression is one of those syntactic sugar features. It leverages a pattern based on a few method calls. In order for a type to be awaitable, it has to meet the following requirements:
- It has to have the following method: INotifyCompletion GetAwaiter()
- Besides implementing the INotifyCompletion interface, the return type of the GetAwaiter method has to have the following: IsCompleted property of type bool, GetResult() method which returns void
If you take a look at <a src="https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task?redirectedfrom=MSDN&view=net-6.0">Task</a> class, you will see that it meets all the above requirements.
<br><br>
So, a type doesn’t even need to implement some specific interface in order to be awaitable. It just has to have a method with a specific signature. It is similar to duck typing.
<br><br>
<i>If it walks like a duck and it quacks like a duck, then it must be a duck.</i>
<br><br>
In this case it is
<br><br>
<i>If it has certain methods with certain signatures, then it has to be awaitable.</i>
<br><br>
To give you an illustrative example of this, I will create some custom class and make it awaitable. So, here is my class:
<pre>
<code>
public class MyAwaitableClass
{

}
</code>
</pre>
When I try to await an object of MyAwaitableClass type, I get the following error:
![image](https://user-images.githubusercontent.com/59767834/143689501-e4455ab3-3b6e-491e-b826-1eaae9d6888a.png)
<br>
It says: 'MyAwaitableClass' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'MyAwaitableClass' could be found (are you missing a using directive or an assembly reference?)<br>
<br>
Let’s add GetAwaiter method to our class:
<pre>
<code>
public class MyAwaitableClass
{
    public MyAwaiter GetAwaiter()
    {
        return new MyAwaiter();
    }
}

public class MyAwaiter
{
    public bool IsCompleted
    {
        get { return false; }
    }
}
</code>
</pre>
<br>
We can see that the compiler error changed:<br>
![image](https://user-images.githubusercontent.com/59767834/143689473-57ed8fdd-e804-408e-a8e8-0a1a9176ad72.png)
<br>
Now it says: 'MyAwaiter' does not implement 'INotifyCompletion'
<br><br>
Ok, let’s create implement the INotifyCompletion interface in MyAwaiter:
<pre>
<code>
public class MyAwaiter : INotifyCompletion
{
    public bool IsCompleted
    {
        get { return false; }
    }

    public void OnCompleted(Action continuation)
    {
    }
}
</code>
</pre>
<br>
and see what the compiler error looks like now:
![image](https://user-images.githubusercontent.com/59767834/143689305-a30f06c6-b318-433b-875b-78914ecd9348.png)
<br>
It says: ‘MyAwaiter’ does not contain a definition for ‘GetResult’
<br><br>
So, we add a GetResult method and now we have the following:
<br>
<pre>
<code>
public class MyAwaitableClass
{
    public MyAwaiter GetAwaiter()
    {
        return new MyAwaiter();
    }
}

public class MyAwaiter : INotifyCompletion
{
    public void GetResult()
    {
    }

    public bool IsCompleted
    {
        get { return false; }
    }

    //From INotifyCompletion
    public void OnCompleted(Action continuation)
    {
    }
}
</code>
</pre>
<br>
And we can also see that there are no compiler errors,<br>

![image](https://user-images.githubusercontent.com/59767834/143689594-5790c585-5a3a-4b2d-af11-5837b5ce4b16.png)

<br>which means we have made an awaitable type.
<br><br>
Now that we know which pattern does the await expression leverage, we can take a look under the hood to see what actually happens when we use async and await.

# Async
For every async method a state machine is generated. This state machine is a struct that implements IAsyncStateMachine interface from System.Runtime.CompilerServices namespace. This interface is intended for compiler use only and has the following methods:
- MoveNext() - Moves the state machine to its next state.
- SetStateMachine(IAsyncStateMachine) - Configures the state machine with a heap-allocated replica.
Now let’s take a look at the following code: <br>
<pre>
<code>
class Program
{
    static void Main(string[] args)
    {
    }

    static async Task FooAsync()
    {
        Console.WriteLine("Async method that doesn't have await");
    }
}
</code>
</pre>
<br>
We have an async method named FooAsync. You may notice that it lacks await operator, but I left it out for now for the sake of simplicity.
<br>
Now let’s take a look at the compiler generated code for this method. I am using dotPeek to decompile the containing .dll file. To see what is going on behind the scenes, you need to enable Show Compiler-generated Code option in dotPeek.
<br>
Compiler generated classes usually contain < and > in their names which are not valid C# identifiers so they don’t conflict with user-created artifacts.
<br>
Let’s take a look what compiler generated for our FooAsync method:<br>
![image](https://user-images.githubusercontent.com/59767834/143689808-12d07465-110f-45d0-809a-729bc40123a6.png)
<br>
Our Program class contains Main and FooAsync methods as expected, but we can also see that compiler generated a struct called Program.<FooAsync>d__1. That struct is a state machine that implements the IAsyncStateMachine interface. Besides the IAsyncStateMachine interface methods, this struct also has the following fields:
