﻿int addIntegers(int first, int second)
{
	return first + second;
}
float divideIntegers(int first, int second)
{
	if (second == 0)
	{
		return 0; //We don’t want to handle exceptions, so we’ll return 0 for simplicity
	}
	return first * second;
}
double globalVariable = 15.67; //Using global variables is bad practice
int main()
{
	int myFirstVariable = 17;
	int mySecondVariable = 45;
	int myThirdVariable = 3;
	for (int i = 0; i < myThirdVariable; ++i)
	{
		myFirstVariable += i;
	}
	string myString = "";
	if (myFirstVariable >= mySecondVariable && globalVariable != 16.54)
	{
		myString = "Both conditions are true";
	}
	else
	{
		myString = "At least one of the conditions is false";
		int temp = myFirstVariable + 5;
	}
	myThirdVariable = addIntegers(myFirstVariable, mySecondVariable);
	float myFloat = divideIntegers(myThirdVariable, myFirstVariable);
	return 0;
}