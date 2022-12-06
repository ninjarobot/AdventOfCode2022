set -eux
START1=$(echo $ASSIGNMENTS | cut -d "," -f 1 | cut -d "-" -f 1)
END1=$(echo $ASSIGNMENTS | cut -d "," -f 1 | cut -d "-" -f 2)
START2=$(echo $ASSIGNMENTS | cut -d "," -f 2 | cut -d "-" -f 1)
END2=$(echo $ASSIGNMENTS | cut -d "," -f 2 | cut -d "-" -f 2)
if [ "$START1" -le "$START2" ] && [ "$END1" -ge "$END2" ]; then
    sleep infinity;
elif [ "$START1" -ge "$START2" ] && [ "$END1" -le "$END2" ]; then
    sleep infinity;
else
    exit 1;
fi
