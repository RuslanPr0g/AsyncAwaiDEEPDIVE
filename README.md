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
![image](https://user-images.githubusercontent.com/59767834/143689121-d42e5b4f-1cf1-4ceb-adac-a0f238f41ecd.png)
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
We can see that the compiler error changed:
<br>
![image](https://user-images.githubusercontent.com/59767834/143689237-ff3a6a27-682f-4462-8954-5a42fc55a445.png)
Now it says: 'MyAwaiter' does not implement 'INotifyCompletion'
<br><br><br>
