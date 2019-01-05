type
    myint = Integer;
    otherInt = type myint;


procedure mem(a: Float; b: Integer; c: myint; d: otherInt);
begin
    a := b  + c + d;
end;


var 
    k,l,m,n : integer;
begin
    mem(k,l,m);
end.