begin
        if condition(a) then 
        begin
            branch(1);
            as := 21;
        end
        else begin
             branch(2);
        
            if nested_condition() then
                os := 22;
        end;
           
end.