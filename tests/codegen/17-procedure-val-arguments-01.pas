type
    arr = array[1..10] of array[1..10] of char;

var i, k: integer;

procedure foo(c:char; g: integer; m:arr);
begin
    writeln(c, g);
    for i := 1 to 10 do
        for k := 1 to 10 do
            writeln(m[i][k]);
end;

var kk:arr;
begin
    for i := 1 to 10 do
        for k := 1 to 10 do
             kk[i][k] := 'z';
                
    foo('a', 12, kk);
end.