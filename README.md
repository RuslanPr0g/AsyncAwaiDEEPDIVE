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
<b>
<br><br>Instantiate the method’s state machine
<br><br>Create new AsyncTaskMethodBuilder and set it as state machine’s builder
<br><br>Set the state machine to a starting state
<br><br>Start the builder with the method’s state machine by calling the Start method.
<br><br>Return the Task
</b>
<br>
<br>
As you can notice, compiler-generated FooAsync method doesn’t contain any of the code our original FooAsync method had. That code represented the functionality of the method. So where is that code? That code is moved to state machine’s MoveNext method. Let’s take a look at Program.<FooAsync>d_1 struct now:
<br>
<pre>
<code>
[CompilerGenerated]
[StructLayout(LayoutKind.Auto)]
private struct <FooAsync>d__1 : IAsyncStateMachine
{
  public int <>1__state;
  public AsyncTaskMethodBuilder <>t__builder;

  void IAsyncStateMachine.MoveNext()
  {
	try
	{
	  Console.WriteLine("Async method that doesn't have await");
	}
	catch (Exception ex)
	{
	  this.<>1__state = -2;
	  this.<>t__builder.SetException(ex);
	  return;
	}
	this.<>1__state = -2;
	this.<>t__builder.SetResult();
  }

  [DebuggerHidden]
  void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
  {
	this.<>t__builder.SetStateMachine(stateMachine);
  }
}
</code>
</pre>
<br>
The MoveNext method contains method’s code inside of a try block. If some exception occurs in our code, it will be given to the method builder which propagates it all the way to the task. After that, it uses method builder’s SetResult method to indicate that the task is completed.
<br>
Now we saw how async methods look under the hood. For the sake of simplicity, I didn’t put any await inside of the FooAsync method, so our state machine didn’t have a lot of state transitions. It just executed our method and went to a completed state, i.e. our method executed synchronously. Now it is time to see how MoveNext method looks like when a method awaits some task inside of its body.

# Await
Let’s take a look at the following method:<br>

![image](https://user-images.githubusercontent.com/59767834/143690508-17a726c9-2b7c-486a-b102-eee4361fa7ff.png)

<br>It awaits some QuxAsync method and uses its task result.
<br>
If we decompile it using dotPeek, we will notice that the compiler generated method has the same structure as FooAsync even if the original methods are different:<br>

![image](https://user-images.githubusercontent.com/59767834/143690564-16dc696b-05f5-4482-a7f2-d46f2af1d72e.png)

<br>What makes the difference is the state machine’s MoveNext method. Now that we have an await expression inside of our method, the state machine loks like this:<br>
<pre>
<code>
[CompilerGenerated]
[StructLayout(LayoutKind.Auto)]
private struct <BarAsync>d__2 : IAsyncStateMachine
{
  public int <>1__state;
  public AsyncTaskMethodBuilder <>t__builder;
  private TaskAwaiter<int> <>u__1;

  void IAsyncStateMachine.MoveNext()
  {
	int num1 = this.<>1__state;
	try
	{
	  TaskAwaiter<int> awaiter;
	  int num2;
	  if (num1 != 0)
	  {
		Console.WriteLine("This happens before await");
		awaiter = Program.QuxAsync().GetAwaiter();
		if (!awaiter.IsCompleted)
		{
		  this.<>1__state = num2 = 0;
		  this.<>u__1 = awaiter;
		  this.<>t__builder.AwaitUnsafeOnCompleted<TaskAwaiter<int>, Program.<BarAsync>d__2>(ref awaiter, ref this);
		  return;
		}
	  }
	  else
	  {
		awaiter = this.<>u__1;
		this.<>u__1 = new TaskAwaiter<int>();
		this.<>1__state = num2 = -1;
	  }
	  Console.WriteLine("This happens after await. The result of await is " + (object) awaiter.GetResult());
	}
	catch (Exception ex)
	{
	  this.<>1__state = -2;
	  this.<>t__builder.SetException(ex);
	  return;
	}
	this.<>1__state = -2;
	this.<>t__builder.SetResult();
  }

  [DebuggerHidden]
  void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
  {
	this.<>t__builder.SetStateMachine(stateMachine);
  }
}
</code>
</pre>
<br>
The following image contains an explanation of the above state machine:<br>

![image](https://user-images.githubusercontent.com/59767834/143690586-23a6413d-40c3-4e63-a0ca-eb103eb62399.png)

<br>So, what await actually does is the following:<br>

![image](https://user-images.githubusercontent.com/59767834/143690597-3ee67fd8-914e-462c-9b20-bcf47ec7d615.png)

<br>Every time you create an async method, the compiler generates a state machine for it. Then for each await inside of that method, it does the following:
<b>
<br>Executes the method to the await expression
<br><br>Checks if the method being awaited has already completed: [If yes, executes the rest of the method] | [If no, uses callback to execute the rest of the method when the method being awaited completes]
</b>
