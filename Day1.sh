set -eux

# Script uses two environment variables
# 1. LINE - the line to read
# 2. MAX_SO_FAR - the max value so far

DATA=/data/Day1.data.txt
NUMLINES=`wc -l < $DATA`
az login --identity
NEXT_LINE=$(( $LINE + 1))
CALORIES=0
if [ "$NEXT_LINE" -le "$NUMLINES" ];
then
    tail -n +$NEXT_LINE $DATA | while read CALORIE
    do
        if [ -z "$CALORIE" ]
        then
            echo "$NEW_MAX" > max.txt
            echo "$NEXT_LINE" > next.txt
            break
        else
            NEXT_LINE=$(( $NEXT_LINE + 1 ))
            CALORIES=$(( $CALORIES + $CALORIE ))
            NEW_MAX=$(( $CALORIES > $MAX_SO_FAR ? $CALORIES : $MAX_SO_FAR ))
            echo "$NEW_MAX" > max.txt
            echo "$NEXT_LINE" > next.txt
        fi
    done
    az storage blob upload --auth-mode login --overwrite -f max.txt -c day1 --account-name advofcode2022
    TEMPLATE=`cat /template/template.json.b64`
    base64 -d /template/template.json.b64 > template.json
    az deployment group cancel -g $RESOURCE_GROUP -n template -o tsv || true
    until az deployment group create -g $RESOURCE_GROUP --template-file template.json --parameters line=`cat next.txt` maxSoFar=`cat max.txt` resourceGroupName=$RESOURCE_GROUP template=$TEMPLATE
    do
        sleep 10
    done
else
    echo "Done!"
fi;
