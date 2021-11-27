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
When I try to await an object of MyAwaitableClass type, I get the following error:<br>

![image](https://user-images.githubusercontent.com/59767834/143690050-8ca2ee79-1a36-4fdc-9fc8-7fa0cc74e718.png)

<br>It says: 'MyAwaitableClass' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'MyAwaitableClass' could be found (are you missing a using directive or an assembly reference?)<br>
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

![image](https://user-images.githubusercontent.com/59767834/143689855-3af02787-84b0-4720-abfc-55f43e406269.png)

<br>Now it says: 'MyAwaiter' does not implement 'INotifyCompletion'
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
and see what the compiler error looks like now:<br>

![image](https://user-images.githubusercontent.com/59767834/143690060-2d5fce15-aa16-4510-a4db-a96c359a3192.png)

<br>It says: ‘MyAwaiter’ does not contain a definition for ‘GetResult’
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
<br><br>
Let’s take a look what compiler generated for our FooAsync method:<br>

![image](https://user-images.githubusercontent.com/59767834/143689866-8260f3b9-19ba-49b3-a73c-7a44cc625d75.png)

<br>Our Program class contains Main and FooAsync methods as expected, but we can also see that compiler generated a struct called Program.<FooAsync>d__1. That struct is a state machine that implements the IAsyncStateMachine interface. Besides the IAsyncStateMachine interface methods, this struct also has the following fields:
    <br><br>
<>1__state which indicates the current state of the state machine
    <br><br>
<>t__builder of type AsyncTaskMethodBuilder which is used for creation of asynchronous methods and returning the resulting task. The AsyncTaskMethodBuilder struct is also intended for use by the compiler.
<br><br><br>
We will see the code of this struct in more detail, but first let’s take a look at what compiler-generated FooAsync method looks like after we decompiled it:
<pre>
<code>
private static Task FooAsync()
{
  Program.<FooAsync>d__1 stateMachine;
  stateMachine.<>t__builder = AsyncTaskMethodBuilder.Create();
  stateMachine.<>1__state = -1;
  stateMachine.<>t__builder.Start<Program.<FooAsync>d__1>(ref stateMachine);
  return stateMachine.<>t__builder.Task;
}
</code>
</pre>
<br>
This is what compiler transforms async methods to. The code inside the method does the following:
<br><br>Instantiate the method’s state machine
<br><br>Create new AsyncTaskMethodBuilder and set it as state machine’s builder
<br><br>Set the state machine to a starting state
<br><br>Start the builder with the method’s state machine by calling the Start method.
<br><br>Return the Task
<br>
<br>
As you can notice, compiler-generated FooAsync method doesn’t contain any of the code our original FooAsync method had. That code represented the functionality of the method. So where is that code? That code is moved to state machine’s MoveNext method. Let’s take a look at Program.<FooAsync>d_1 struct now:
