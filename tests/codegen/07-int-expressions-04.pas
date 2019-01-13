var
    a, b, c: integer;
begin
    a := 10;
    b := 20;
    c := a*b + (a - b) + b div a;
    writeln(a, ' ' , b, ' ' , c);
    writeln(1234 mod 10);
    writeln(13 mod 7);
    writeln(-13 mod 7);

    writeln(2+2*2-(3+3), ' ' , 10 div 2*3);
    writeln((100-200) div 10);

    writeln(200*200 div -200 + 21);
end.