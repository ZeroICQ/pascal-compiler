var
    a: array [0..100] of integer;
begin
    a[10] := 20;
    a[9] := a[5 + 5] - 10;
    writeln(a[9]);
    writeln(a[10]);
end.