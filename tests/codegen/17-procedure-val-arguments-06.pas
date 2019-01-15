
var a : array [0..3] of integer;
var i : integer;

procedure foo;
var i : integer;
var ar : array[0..3] of char;
var foo : integer;
begin
    i := 123;
    writeln(i);
    ar[0] := 'F';
    ar[1] := 'o';
    ar[2] := 'o';
    ar[3] := '2';
    writeln('Foo1 ', i);
    foo := 12;
    for i := 0 to 3 do begin
        write(ar[i], ' ');    
    end;
    writeln();
    for i := 0 to 3 do begin 
        a[i] := i;
        write(a[i], ' '); 
    end;
    i := 123;
    writeln();
    foo := i * 2 * 2;
    write(foo);

end;




begin
    foo();

end.