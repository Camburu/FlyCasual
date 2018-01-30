﻿using System;
using System.Collections.Generic;
using System.Linq;
using Tokens;
using UnityEngine;

namespace Ship
{
    public class TokensManager
    {
        private GenericShip Host;
        private List<GenericToken> AssignedTokens = new List<GenericToken>();
        public GenericToken TokenToAssign;

        public TokensManager(GenericShip host)
        {
            Host = host;
        }

        public List<GenericToken> GetAllTokens()
        {
            return AssignedTokens;
        }

        public bool HasToken(Type type, char letter = ' ')
        {
            return GetToken(type, letter) != null;
        }

        public int TokenCount(Type type)
        {
            return GetAllTokens().Count(n => n.GetType() == type);
        }

        public GenericToken GetToken(Type type, char letter = ' ')
        {
            GenericToken result = null;

            foreach (var assignedToken in AssignedTokens)
            {
                if (assignedToken.GetType() == type)
                {
                    if (assignedToken.GetType().BaseType == typeof(GenericTargetLockToken))
                    {
                        if (((assignedToken as GenericTargetLockToken).Letter == letter) || (letter == '*'))
                        {
                            return assignedToken;
                        }
                    }
                    else
                    {
                        return assignedToken;
                    }
                }
            }
            return result;
        }

        public GenericTargetLockToken GetTargetLockToken(char letter)
        {
            return (GenericTargetLockToken)AssignedTokens.Find(n => n.GetType().BaseType == typeof(GenericTargetLockToken) && (n as GenericTargetLockToken).Letter == letter);
        }

        public char GetTargetLockLetterPair(GenericShip targetShip)
        {
            char result = ' ';

            GenericToken blueToken = GetToken(typeof(BlueTargetLockToken), '*');
            if (blueToken != null)
            {
                char foundLetter = (blueToken as BlueTargetLockToken).Letter;

                GenericToken redToken = targetShip.Tokens.GetToken(typeof(RedTargetLockToken), foundLetter);
                if (redToken != null)
                {
                    return foundLetter;
                }
            }
            return result;
        }

        public void AssignToken(GenericToken token, Action callBack, char letter = ' ')
        {
            TokenToAssign = token;

            Host.CallBeforeAssignToken(
                TokenToAssign,
                delegate { FinalizeAssignToken(callBack); }
            );
        }

        private void FinalizeAssignToken(Action callback)
        {
            if (TokenToAssign == null)
            {
                callback();
                return;
            }

            AssignedTokens.Add(TokenToAssign);

            Host.CallOnTokenIsAssigned(TokenToAssign, callback);
        }

        public void RemoveCondition(GenericToken token)
        {
            if (AssignedTokens.Remove(token))
            {
                Host.CallOnConditionIsRemoved(token.GetType());
            }
        }

        public void RemoveCondition(System.Type type)
        {
            GenericToken assignedCondition = GetToken(type);
            RemoveCondition(assignedCondition);
        }

        public void RemoveToken(System.Type type, Action callback, char letter = ' ')
        {
            GenericToken assignedToken = GetToken(type, letter);

            if (assignedToken == null)
            {
                callback();
            }
            else
            {
                RemoveToken(assignedToken, callback, letter);
            }
        }

        public void RemoveToken(GenericToken tokenToRemove, Action callback, char letter = ' ')
        {
            AssignedTokens.Remove(tokenToRemove);

            if (tokenToRemove.GetType().BaseType == typeof(GenericTargetLockToken))
            {
                GenericShip otherTokenOwner = (tokenToRemove as GenericTargetLockToken).OtherTokenOwner;
                Actions.ReleaseTargetLockLetter((tokenToRemove as GenericTargetLockToken).Letter);
                Type oppositeType = (tokenToRemove.GetType() == typeof(BlueTargetLockToken)) ? typeof(RedTargetLockToken) : typeof(BlueTargetLockToken);

                GenericToken otherTargetLockToken = otherTokenOwner.Tokens.GetToken(oppositeType, letter);
                if (otherTargetLockToken != null)
                {
                    otherTokenOwner.Tokens.GetAllTokens().Remove(otherTargetLockToken);
                    otherTokenOwner.CallOnRemoveTokenEvent(otherTargetLockToken.GetType());
                }
            }

            Host.CallOnRemoveTokenEvent(tokenToRemove.GetType());

            Triggers.ResolveTriggers(TriggerTypes.OnTokenIsRemoved, callback);
        }

        public void RemoveAllTokensByType(Type tokenType, Action callback)
        {
            GenericToken tokenToRemove = GetToken(tokenType);
            if (tokenToRemove != null)
            {
                RemoveToken(
                    tokenType,
                    delegate { RemoveAllTokensByType(tokenType, callback); }
                );
            }
            else
            {
                callback();
            }
        }

        public void SpendToken(Type type, Action callback, char letter = ' ')
        {
            RemoveToken(
                type,
                delegate { Host.CallFinishSpendToken(type, callback); },
                letter
            );
        }

    }
}