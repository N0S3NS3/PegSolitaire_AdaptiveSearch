using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;
using PegSolitair.HelperBase;

namespace PegSolitair
{
    class LogicBase
    {
        public enum Difficulty
        {
            Dumb,
            Somewhat,
            Very
        };
        private int Depth { get; set; }
        private int SearchTimer { get; set; }       //used in adaptive depth bounding
        private int TotalMoves { get; set; }
        private int MaxDepth { get; set; }
        private TimeSpan startTime { get; set; }
        private TimeSpan endTime { get; set; }
        private Difficulty GameDifficulty { get; set; }
        private Thread SearchThread { get; set; }

        public void InitializeLogic(Difficulty difficulty)
        {
            SearchTimer = 0;
            TotalMoves = 0;
            GameDifficulty = difficulty;
            startTime = new TimeSpan();
            endTime = new TimeSpan();
        }
        public int[][] HandleAIMove(GameBucket<int, int, SolitairBase.Piece>[][] currentBoard)
        {
            int[] rootNode = new int[currentBoard.Length * currentBoard.Length];
            int[] bestMove = new int[81];
            int elem = 0;
            for (int x = 0; x < currentBoard.Length; x++)
            {
                for (int y = 0; y < currentBoard.Length; y++)
                {
                    if (currentBoard[x][y] == null)
                        rootNode[elem++] = -1;
                    else
                        rootNode[elem++] = currentBoard[x][y].Item1;
                }
            }
            switch (GameDifficulty)
            {
                case Difficulty.Dumb:
                    Depth = 2;
                    MaxDepth = 2;
                    SearchThread = new Thread(() => Dumb(Depth, true, Int32.MaxValue, Int32.MinValue, rootNode, ref bestMove));
                    break;
                case Difficulty.Somewhat:
                    Depth = 2;
                    MaxDepth = 3;
                    SearchThread = new Thread(() => Somewhat(Depth, true, Int32.MaxValue, Int32.MinValue, rootNode, ref bestMove));
                    break;
                case Difficulty.Very:
                    Depth = 9;
                    MaxDepth = 20;
                    SearchThread = new Thread(() => Very(Depth, true, Int32.MaxValue, Int32.MinValue, rootNode, ref bestMove));
                    break;
            }

            TimeSpan first = DateTime.Now.TimeOfDay;
            SearchThread.Start();
            SearchThread.Join();
            Console.WriteLine("Initial MaxDepth: " + Depth);
            Console.WriteLine("totalMoves = " + TotalMoves);
            TimeSpan end = DateTime.Now.TimeOfDay;
            SearchTimer = end.Subtract(first).Seconds;

            //some adaptiveBounds features follow:
            int _depth = Depth;
            while (SearchTimer <= 5 && Depth < MaxDepth)
            {
                Depth += 1;
                Console.WriteLine("Best Move = " + bestMove[0] + " to " + bestMove[2]);
                Console.WriteLine("Search ended too quickly: deepening search to maxDepth: " + (Depth));
                switch (GameDifficulty)
                {
                    case Difficulty.Dumb:
                        SearchThread = new Thread(() => Dumb(Depth, true, Int32.MaxValue, Int32.MinValue, rootNode, ref bestMove));
                        break;
                    case Difficulty.Somewhat:
                        SearchThread = new Thread(() => Somewhat(Depth, true, Int32.MaxValue, Int32.MinValue, rootNode, ref bestMove));
                        break;
                    case Difficulty.Very:
                        SearchThread = new Thread(() => Very(Depth, true, Int32.MaxValue, Int32.MinValue, rootNode, ref bestMove));
                        break;
                }
                SearchThread.Start();
                SearchThread.Join();
                end = DateTime.Now.TimeOfDay;
                SearchTimer += end.Subtract(first).Seconds;//last move timer = total search time so far
            }
            if (bestMove.Length > 3)
            {
                return null;    //No more possible moves on board
            }
            Console.WriteLine("Total time taken: " + SearchTimer);
            Console.WriteLine("Best Move = " + bestMove[0] + " to " + bestMove[2]);
            TotalMoves = 0;
            int[][] translatedMoveSet = new int[3][];
            for (int i = 0; i < translatedMoveSet.Length; i++)
            {
                translatedMoveSet[i] = new int[] { bestMove[i] / 9, bestMove[i] % 9 };
            }
            Depth = _depth;
            return translatedMoveSet;
        }
        public int Dumb(int depth, bool isMax, int min, int max, int[] node, ref int[] bestMove)
        {
            TotalMoves++;
            if (depth == 0)//if at leaf node (root = maxDepth)
            {
                return DumbEvaluation(node);
            }
            int[] bestNode = new int[81];
            foreach (int[] possibleMoves in GetChildren(node, depth))
            {//calculates and receives one move at a time for every possible move in 'node'
                int currentScore = Dumb(depth - 1, !isMax, min, max, TranslateNode(possibleMoves, node), ref bestMove);
                //recursive call of Dumb() will travel from current node to each child node until all moves are evaluated
                //this is where all backtracking occurs
                if (isMax) //MaxPlayer
                {
                    if (currentScore > max)
                    {//we have found a better max
                        max = currentScore;  //setting aplha value
                        //Console.WriteLine("depth = " + depth +" new max = "+ max);
                        bestNode = possibleMoves;
                        if (depth == Depth)
                        {
                            bestMove = possibleMoves;
                        }
                    }
                }
                if (!isMax) //MinPlayer
                {
                    if (currentScore < min)
                    {//we have found a lower min
                        min = currentScore;//setting beta value
                        bestNode = possibleMoves;
                        if (depth == Depth)
                        {
                            bestMove = possibleMoves;
                        }
                    }
                }
            }
            //has exhausted all moves for a node
            return isMax ? max : min;
        }
        public int Somewhat(int depth, bool isMax, int min, int max, int[] node, ref int[] bestMove)
        {
            //Console.WriteLine("HELLO: I AM SMART, PREPARE FOR DESIMATION YE FIENDISH PEGS");
            TotalMoves++;
            if (depth == 0)//if at leaf node (root = maxDepth)
            {
                return SomewhatEvaluation(node);
            }
            int[] bestNode = new int[81];
            foreach (int[] possibleMoves in GetChildren(node, depth))
            {//calculates and receives one move at a time for every possible move in 'node'
                int currentScore = Somewhat(depth - 1, !isMax, min, max, TranslateNode(possibleMoves, node), ref bestMove);
                if (depth == Depth) { bestMove = possibleMoves; }
                if (isMax) //MaxPlayer
                {
                    if (currentScore > max)
                    {//we have found a better max
                        max = currentScore;  //setting aplha value
                        //Console.WriteLine("depth = " + depth +" new max = "+ max);
                        bestNode = possibleMoves;
                        if (depth == Depth)
                        {
                            bestMove = possibleMoves;
                        }
                        bestMove = possibleMoves;
                    }
                    if (max >= min) //THIS 'IF' PROVIDES PRUNING FUNCTIONALITY FOR MAX NODES
                    {//if alpha > beta
                        // Console.WriteLine(max + ">=" + min + "Pruning the rest of this branch and backtracking...");
                        return max; //return alpha (cut off rest of search for this branch)
                    }
                }
                if (!isMax) //MinPlayer
                {
                    if (currentScore < min)
                    {//we have found a lower min
                        //Console.WriteLine("Pruning the rest of this branch and backtracking...");
                        min = currentScore;//setting beta value
                        bestNode = possibleMoves;
                        if (depth == Depth)
                        {
                            bestMove = possibleMoves;
                        }
                    }
                    if (min <= max) //THIS 'IF' PROVIDES PRUNING FUNCTIONALITY FOR MIN NODES
                    {//if beta < alpha
                        //  Console.WriteLine(min + "<=" + max + ", Pruning the rest of this branch and backtracking...");
                        return min; //return beta (cut off rest of search for this branch)
                    }
                }
            }
            //has exausted all moves for a node
            return isMax ? max : min;
        }
        public int Very(int depth, bool isMax, int min, int max, int[] node, ref int[] bestMove)
        {
            //Console.WriteLine("HELLO: I AM SMART, PREPARE FOR DESIMATION YE FIENDISH PEGS");
            TotalMoves++;
            if (depth == 0)//if at leaf node (root = maxDepth)
            {
                return SmartEvaluation(node);
            }
            int[] bestNode = new int[81];
            foreach (int[] possibleMoves in GetChildren(node, depth))
            {//calculates and receives one move at a time for every possible move in 'node'
                int currentScore = Very(depth - 1, !isMax, min, max, TranslateNode(possibleMoves, node), ref bestMove);
                if (isMax) //MaxPlayer
                {
                    if (currentScore > max)
                    {//we have found a better max
                        max = currentScore;  //setting aplha value
                        //Console.WriteLine("depth = " + depth +" new max = "+ max);
                        bestNode = possibleMoves;
                        if (depth == Depth) 
                        { 
                            bestMove = possibleMoves; 
                        }
                    }
                    if (max >= min) //THIS 'IF' PROVIDES PRUNING FUNCTIONALITY FOR MAX NODES
                    {//if alpha > beta
                        // Console.WriteLine(max + ">=" + min + "Pruning the rest of this branch and backtracking...");
                        return max; //return alpha (cut off rest of search for this branch)
                    }
                }
                if (!isMax) //MinPlayer
                {
                    if (currentScore < min)
                    {//we have found a lower min
                        //Console.WriteLine("Pruning the rest of this branch and backtracking...");
                        min = currentScore;//setting beta value
                        bestNode = possibleMoves;
                        if (depth == Depth)
                        {
                            bestMove = possibleMoves;
                        }
                    }
                    if (min <= max) //THIS 'IF' PROVIDES PRUNING FUNCTIONALITY FOR MIN NODES
                    {//if beta < alpha
                        //  Console.WriteLine(min + "<=" + max + ", Pruning the rest of this branch and backtracking...");
                        return min; //return beta (cut off rest of search for this branch)
                    }
                }
            }
            //has exausted all moves for a node
            return isMax ? max : min;
        }
        private IEnumerable<int[]> GetChildren(int[] node, int depth)
        {
            //computes and returns a single possible move
            //into an enumarable array of ints(a node).
            //each yield return adds one move to 

            //this is where the logic for only looking at 16 pieces to update WOULD come in
            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    int index = y * 9 + x;
                    if (node[index] == 1)//if there is a piece at the current coordinate
                    {
                        int adjacentWest = (y - 1) * 9 + x, moveWest = (y - 2) * 9 + x;
                        int adjacentSouth = y * 9 + (x + 1), moveSouth = y * 9 + (x + 2);
                        int adjacentEast = (y + 1) * 9 + x, moveEast = (y + 2) * 9 + x;
                        int adjacentNorth = y * 9 + (x - 1), moveNorth = y * 9 + (x - 2);
                        if ((y - 2 > 0) && (node[adjacentWest] == 1) && (node[moveWest] == 0))
                        {
                            yield return new int[] { index, adjacentWest, moveWest };
                        }
                        if ((x + 2 < 9) && (node[adjacentSouth] == 1) && (node[moveSouth] == 0))
                        {
                            yield return new int[] { index, adjacentSouth, moveSouth };
                        }
                        if ((y + 2 < 9) && (node[adjacentEast] == 1) && (node[moveEast] == 0))
                        {
                            yield return new int[] { index, adjacentEast, moveEast };
                        }
                        if ((x - 2 > 0) && (node[adjacentNorth] == 1) && (node[moveNorth] == 0))
                        {
                            yield return new int[] { index, adjacentNorth, moveNorth };
                        }
                    }
                }
            }
        }
        private int[] TranslateNode(int[] possibleMove, int[] currentNode)
        {
            int[] newNode = new int[currentNode.Length];
            Array.Copy(currentNode, newNode, currentNode.Length);
            newNode[possibleMove[0]] = 0;
            newNode[possibleMove[1]] = 0;
            newNode[possibleMove[2]] = 1;
            return newNode;
        }
        private int DumbEvaluation(int[] node)
        {// high value = more weighted 
            //Dumb version only looks for win/loss states
            int moveCount = 0;
            foreach (int[] validMove in GetChildren(node, 0)) //0 parameter is arbitrary
            {
                moveCount++;
            }
            if (moveCount == 0)
                return 1000;
            else
                return moveCount;
        }
        private int SomewhatEvaluation(int[] node)
        {// high value = more weighted 
            //Somewhat intelligent version looks for win/loss states, 
            //if none are found it looks for 
            int moveCount = 0;
            foreach (int[] validMove in GetChildren(node, 0)) //0 parameter is arbitrary
            {
                moveCount++;
            }
            if (moveCount == 0)
                return 1000; //endstate
            if (moveCount == 1)
            {
                if (checkForSweep(node))//does this node lead to an unavoidable string of one moves til endgame?
                    return 900;
            }
            return 50 - moveCount;
        }
        private int SmartEvaluation(int[] node)
        {
            // high value = more weighted 
            //smart evaluation looks for win/loss states, 
            //if none are found it looks for sweeps
            //if none are found it weighs the node by number of moves where more move is more desirable
            //We look to minimize number of possible moves from a location in order to ba able to find sweeps faster
            //A huge problem for many complex evaluation functions is that it will have 
            //  negative impacts on our performance, and since our main focus from the 
            //  beginning has been speed, we need to strike a balance here...
            int moveCount = 0;
            foreach (int[] validMove in GetChildren(node, 0)) //0 parameter is arbitrary
            {
                moveCount++;
            }
            if (moveCount == 0)
                return 1000; //endstate
            else if (moveCount <= 2)
            {//does this node lead to an unavoidable string of one/two move gameboards until endgame?
                int sweepCheck = checkForSweepSmarter(node, 0);
                if (sweepCheck == 1)
                    return 900; //leads to an unavoidable win for the player who calls evaluation
                else if (sweepCheck == 0)
                    return 50 - moveCount; //inconclusive answers return normal eval
                else
                    return 0;//leads to an unavoidable loss for player who calls evaluation
            }
            //other heuristics have been inconclusive if it has got this far... so let's just attempt to confuse everything...
            return 50 - moveCount; //more moves at root = more desirable for our search, given that (hopefully) our main advantage is speed.
        }

        private Boolean checkForSweep(int[] node)
        {//this method will go beyond depth bounds to search for a possible endgame sweep 
            int moveCount = 0;
            int[] move = { 0, 0, 0 };
            foreach (int[] validMove in GetChildren(node, 0)) //0 parameter is arbitrary
            {
                move = validMove;
                moveCount++;
                if (moveCount > 2)
                    return false;//the sweep devolves into a more complex branching pattern
            }
            if (moveCount == 0)
            {//this examined sweep continues until endgame
                return true;
            }
            if (moveCount == 1)
            {
                int[] newNode = TranslateNode(move, node);
                checkForSweep(newNode);
            }
            return false;
        }

        private int checkForSweepSmarter(int[] node, int depth)
        {//this method will go beyond depth bounds to search for a possible endgame sweep 
            //returns 0 for an inconclusive sweep | -1 for losing sweep | +1 for winning sweep
            int[] move = { 0, 0, 0 };
            int moveCount = 0;
            foreach (int[] validMove in GetChildren(node, 0)) //0 parameter is arbitrary
            {
                move = validMove;//if moveCount == 2, an overwrite here is not a problem as it's rehandled later below
                moveCount++;
                if (moveCount > 2)
                {
                    return 0;//the sweep devolves into a more complex branching patterns (no conclusive results)
                }
            }
            if (moveCount == 0)
            {//this examined sweep continues until endgame
                if (depth % 2 == 0)
                {
                    return -1; //if endstate found here it is a loss. return bad eval #(-1) this branch could defeat us
                }
                else
                {
                    return 1; //this endstate on the other hand could be our deep win return good eval (1)
                }
            }
            if (moveCount == 1)
            {
                int[] newNode = TranslateNode(move, node);
                return checkForSweepSmarter(newNode, depth + 1);
            }
            if (moveCount == 2)
            {
                foreach (int[] validMove in GetChildren(node, 0)) //0 parameter is arbitrary
                {
                    move = validMove;
                    int[] newNode = TranslateNode(move, node);
                    int branchSweepVar = checkForSweepSmarter(newNode, depth + 1);
                    if (branchSweepVar == -1)
                        return -1;
                    if (branchSweepVar == 0)
                        return 0;
                    //shortcoming... if first branch evaluates to 0 and next to -1 we will never know of the -1 and wont forsee that possible loss until minimax picks it up
                    //so if it has not returned after both moves are evaluated (at this point), then both branches end with win states!
                }
                return 1;
            }
            return 0;
        }
        private int getMoveCount(int[] node)
        {
            int moveCount = 0;
            foreach (int[] validMove in GetChildren(node, 0))   //0 parameter is arbitrary
            {
                moveCount++;
            }
            return moveCount;
        }
        private void PrintNode(int[] nodeToPrint)               //For Debug Purposes
        {
            Console.Write("+012345678");
            for (int y = 0; y < 9; y++)
            {
                Console.Write("\n" + y);
                for (int x = 0; x < 9; x++)
                {
                    int index = x * 9 + y;
                    if (nodeToPrint[index] == -1)
                        Console.Write("-");
                    else
                        Console.Write(nodeToPrint[index]);
                }
            }
            Console.Write("\n");
        }
    }
}