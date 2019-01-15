const
    a = 10;
var
    b: integer = 120;
begin
    writeln(a = 10);
    writeln(a <> 10);

    writeln(a > 9);
    writeln(a > 11);

    writeln(a < 11);
    writeln(a < 9);

    writeln(b = 110);
    
    writeln(b >= a);
    writeln(b >= a + b);

    writeln(b <= a*100);
    writeln(b <= 0);

    writeln( ' ' );
    writeln(102 > 101);
    writeln(102 >= 101);
    writeln(101 < 102);
    writeln(101 <= 102);
    writeln(101 = 101);

    writeln(102 < 101);
    writeln(102 <= 101);
    writeln(101 > 102);
    writeln(101 >= 102);
    writeln(101 <> 101);

    writeln( ' ' );
    writeln(-102 > -101);
    writeln(-102 >= -101);
    writeln(-101 < -102);
    writeln(-101 <= -102);
    writeln(-101 <> -101);

    writeln(-102 < -101);
    writeln(-102 <= -101);
    writeln(-101 > -102);
    writeln(-101 >= -102);
    writeln(-101 = -101);
end.