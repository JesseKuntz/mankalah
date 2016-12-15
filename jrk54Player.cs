using System;
using System.Diagnostics;

namespace Mankalah
{
    /*****************************************************************/
    /* Name: Jesse Kuntz    Date: 10/12/16
    /* Program: 5, Mankalah AI
    /* Starting code provided by: Professor Plantinga
    /*
    /* jrk54Player.cs contains an intelligent Mankalah player named 
    /* CHAPPiE. He is a robot.
    /*****************************************************************/
    class Chappie : Player
    {
        // constructor
        public Chappie(Position pos, int timeLimit) : base(pos, "CHAPPiE", timeLimit) { }

        // Overriden chooseMove() function, which contains the code to check for time to make
        // sure that it is a staged DFS and the code to run the minimax function as well as 
        // return the move that is best calculated.
        public override int chooseMove(Board b)
        {   
            Stopwatch watch = new Stopwatch();
            watch.Start();
            int i = 1;
            MoveResult move = new MoveResult(0, 0, false);
            // try-catch block to make sure that we are not only using the latest 
            // COMPLETED move, but also check that we have not passed the time limit
            try 
            {
                while (!move.isEndGame()) 
                {
                    move = minimax(b, i++, watch, Int32.MinValue, Int32.MaxValue);
                }
            }
            // if we see that we are past the time, don't do anything and return the move
            catch (MoveTimedOutException)
            {
                // NOP
            }
            return move.getMove();
        }

        // The function minimax() calculates the best possible move that Chappie can make by 
        // recursing as far as it can within the time limit and keeping track of what the best move and 
        // score are as it recurses. It is also implemented with AB pruning, meaning that if it 
        // starts to go down a path where the outcome will inevitable be worse that our best values,
        // it prunes off that path, therefore saving time to go down a better path.
        private MoveResult minimax(Board b, int d, Stopwatch w, int alpha, int beta)
        {
            // check to see if the time limit is up
            if (w.ElapsedMilliseconds > getTimePerMove()) throw new MoveTimedOutException();
            // base case
            if (b.gameOver() || d == 0)
            {               
                return new MoveResult(0, evaluate(b), b.gameOver());
            }
            // initialization of trackers
            int bestMove = 0;
            int bestVal;
            bool gameCompleted = false;
            // check all the the moves that top could make, and act as if it is the MAX in minimax
            if (b.whoseMove() == Position.Top) // TOP is MAX
            {
                // smallest possible value so that it can only get better
                bestVal = Int32.MinValue;
                for (int move = 7; move <= 12 && alpha < beta; move++)
                {
                    if (b.legalMove(move))
                    {
                        Board b1 = new Board(b);                               // duplicate board
                        b1.makeMove(move, false);                              // make the move
                        MoveResult val = minimax(b1, d - 1, w, alpha, beta);   // find its value
                        if (val.getScore() > bestVal)                          // remember if best
                        {
                            bestVal = val.getScore();
                            bestMove = move;
                            // track the current condition of the game
                            gameCompleted = val.isEndGame();
                        }
                        // prune
                        if (bestVal > alpha) {
                            alpha = bestVal;
                        }
                    }
                }
            }
            // check all the the moves that bottom could make, and act as if it is the MIN in minimax
            else  // BOTTOM is MIN
            {
                // lergest possible value so that it can only get better
                bestVal = Int32.MaxValue;
                for (int move = 0; move <= 5 && alpha < beta; move++)
                {
                    if (b.legalMove(move))
                    {
                        Board b1 = new Board(b);                               // duplicate board
                        b1.makeMove(move, false);                              // make the move
                        MoveResult val = minimax(b1, d - 1, w, alpha, beta);   // find its value
                        if (val.getScore() < bestVal)                          // remember if best
                        {
                            bestVal = val.getScore();
                            bestMove = move;
                            // track the current condition of the game
                            gameCompleted = val.isEndGame();
                        }
                        // prune
                        if (bestVal < beta) {
                            beta = bestVal;
                        }
                    }
                }
            }
            return new MoveResult(bestMove, bestVal, gameCompleted);
        }

        // The overriden evaluate() function checks various features of the current board, and 
        // attempts to predict the score in order to assisst the minimax function in 
        // choosing the best move to make. The score will generally be negative if 
        // bottom is predicted to win, and positive if top is predicted to win.
        public override int evaluate(Board b)
        {
            // heuristics
            int score = b.stonesAt(13) - b.stonesAt(6); // necessary, tested as one of the best heuristics
            int stonesTotal = 0; // established that it is very good, both by tests and articles (1)
            int goAgainsTotal = 0; // established as accurate, it will add an extra point if a 'go-again' is possible (2)
            int capturesTotal = 0; // established as accurate, it adds the actual number of points that would be captured (3)
            int opponentWinning = 0; // not entirely accurate, the weights are subject to change (4)

            // TOP loop - calcuate heuristics for the top player
            for (int i = 7; i <= 12; i++)
            {
                // add all the stones in the top row
                stonesTotal += b.stonesAt(i);
                // add all the 'go-again's in the top row
                if (b.stonesAt(i) - (13 - i) == 0) goAgainsTotal += 1;

                // add all of stones that can be obtained through captures
                int target = i + b.stonesAt(i);
                if (target < 13)
                {
                    int targetStones = b.stonesAt(target);
                    if (b.whoseMove() == Position.Top) {
                        if (targetStones == 0 && b.stonesAt(13 - target - 1) != 0) capturesTotal += b.stonesAt(13 - target - 1);
                    }
                }                
            }

            // BOTTOM loop - calcuate heuristics for the bottom player
            for (int i = 0; i <= 5; i++)
            {
                // subtract all the stones in the bottom row
                stonesTotal -= b.stonesAt(i);
                // add all the 'go-again's in the bottom row
                if (b.stonesAt(i) - (6 - i) == 0) goAgainsTotal -= 1;

                // subtract all of stones that can be obtained through captures
                int target = i + b.stonesAt(i);
                if (target < 6)
                {
                    int targetStones = b.stonesAt(target);
                    if (b.whoseMove() == Position.Bottom) {
                        if (targetStones == 0 && b.stonesAt(13 - target - 1) != 0) capturesTotal -= b.stonesAt(13 - target - 1);
                    }
                }                
            }

            // calculate the 'closeness' of the opponent winning
            if (b.whoseMove() == Position.Top) {
                // if you are top and your opponent is close to winning, give some points to MIN
                if (b.stonesAt(6) > 24) opponentWinning -= 3;
            }
            else {
                // if you are bottom and your opponent is close to winning, give some points to MAX
                if (b.stonesAt(13) > 24) opponentWinning += 3;
            }

            // add up all of the heuristics and return what is believed the score will be
            score += stonesTotal + capturesTotal + opponentWinning + goAgainsTotal;
            return score;
        }

        // overriden getImage() function to return an image of myself
        public override String getImage() { return "jesse.png"; }

        // overriden gloat() function to spit out a customized gloat
        public override String gloat() { return "I'm consciousness. I'm alive. I'm Chappie."; }
    }

    class MoveTimedOutException : Exception { }

    /*****************************************************************/
    /*
    /* MoveRseult is a class to store the results of the minimax 
    /* function. It holds both the best move and the score for that move.
    /*
    /*****************************************************************/

    class MoveResult
    {
        private int bestMove;
        private int bestScore;
        private bool endGame;
        public MoveResult(int move, int score, bool end)
        {
            bestMove = move;
            bestScore = score;
            endGame = end;
        }

        public int getMove() { return bestMove; }
        
        public int getScore() { return bestScore; }

        public bool isEndGame() { return endGame; }
    }
}