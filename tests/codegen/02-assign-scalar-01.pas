var i1, i2 : integer;
    f1, f2 : double;
    c1, c2 : char;

begin
    i1 := 1337;
    i2 := 78;
    f1 := 22.9;
    f2 := 20;
    c1 := 'z';
    c2 := 'a';
    
    writeln(i1);
    writeln(i2);
    writeln(f1);
    writeln(f2);
    writeln(c1);
    writeln(c2);
    
    i2 := i1;
    f2 := f1;
    c2 := c1;
    writeln;
    writeln(i1);
    writeln(i2);
    writeln(f1);
    writeln(f2);
    writeln(c1);
    writeln(c2);
end.