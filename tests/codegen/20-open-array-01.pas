function foo(var a: array of char) : char;
var 
    i: integer;
begin
    for i := low(a) to high(a) do
        writeln(a[i]);
end;

var b : array[10..20] of char; 

begin
    //writeln(high(b));
    b[15] := 's';
    foo(b);
end.    