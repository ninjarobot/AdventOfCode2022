set -eux
START1=$(echo $ASSIGNMENTS | cut -d "," -f 1 | cut -d "-" -f 1)
END1=$(echo $ASSIGNMENTS | cut -d "," -f 1 | cut -d "-" -f 2)
START2=$(echo $ASSIGNMENTS | cut -d "," -f 2 | cut -d "-" -f 1)
END2=$(echo $ASSIGNMENTS | cut -d "," -f 2 | cut -d "-" -f 2)
seq $START1 $END1 | sort > first
seq $START2 $END2 | sort > second
OVERLAP=$(comm -12 first second)
if [ -z "$OVERLAP" ]; then # nothing overlaps, exit it as an error
    exit 1;
else # some numbers overlapped, let it remain running
    sleep infinity;
fi
