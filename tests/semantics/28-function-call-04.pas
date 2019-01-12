type
    myint = Integer;
    otherInt = type myint;


procedure mem(a: Double; b: Integer; c: myint; d: otherInt);
begin
    a := b  + c + d;
end;


var 
    k,l,m,n : integer;
begin
    m := mem(1, 2, 3, 2);
end.