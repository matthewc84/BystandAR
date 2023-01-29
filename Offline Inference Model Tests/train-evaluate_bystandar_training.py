import pandas as pd
from sklearn import tree
from sklearn.preprocessing import LabelEncoder, StandardScaler
from sklearn.model_selection import train_test_split
from sklearn.metrics import accuracy_score, classification_report, confusion_matrix
#import graphviz
import xgboost
from sklearn import svm
from sklearn.model_selection import GridSearchCV
from sklearn.model_selection import KFold,cross_val_score
from sklearn.neural_network import MLPClassifier
from sklearn.ensemble import RandomForestClassifier


def main():
    #df = pd.read_excel('features.xlsx', header=0)
    train_df = pd.read_excel('day1_test1_features.xlsx', header=0)
    lb = LabelEncoder()
    lb.fit(train_df['label'])
    label_col = lb.fit_transform(train_df['label'])
    train_df['label'] = label_col
    train_df.drop(labels=['center_deviation'], axis=1, inplace=True)
    print(train_df.describe())
    print(train_df['label'].value_counts())
    x_train_df = train_df.iloc[:, :-1]
    y_train_df = train_df.iloc[:, -1]
    
    #x_train, x_test, y_train, y_test = train_test_split(x_train_df, y_train_df, test_size=0.1, shuffle=True)


    # train xgboost classifier
    parameters={'n_estimators':[100,200,300,400,500],'min_child_weight': [1, 5, 10],'subsample': [0.6, 0.8, 1.0],'colsample_bytree': [0.6, 0.8, 1.0],'max_depth': [6,8,10]}
    xgb = xgboost.XGBClassifier(max_depth=10, n_estimators=300, learning_rate=0.03, silent=True, subsample=0.7,
                                objective='binary:logistic') 
    
    
    
    xgb.fit(X=x_train_df, y=y_train_df)
    
    #load the two other bystandar test runs into list and concat for now 
    test2_df = pd.read_excel('day1_test2_features.xlsx', header=0)
    lb = LabelEncoder()
    lb.fit(test2_df['label'])
    label_col = lb.fit_transform(test2_df['label'])
    test2_df['label'] = label_col
    test2_df.drop(labels=['center_deviation'], axis=1, inplace=True)
    
    test3_df = pd.read_excel('day1_test3_features.xlsx', header=0)
    lb = LabelEncoder()
    lb.fit(test3_df['label'])
    label_col = lb.fit_transform(test3_df['label'])
    test3_df['label'] = label_col
    test3_df.drop(labels=['center_deviation'], axis=1, inplace=True)
    
    test_df = pd.concat([test2_df,test3_df],ignore_index=True)
    x_test_df = test_df.iloc[:, :-1]
    y_test_df = test_df.iloc[:, -1]

    #todo: if want to use the prediction back into features for bounding boxes, etc. do prediction by test 
    
    y_predict = xgb.predict(x_test_df)
    print('Accuracy score: ' + str(accuracy_score(y_true=y_test_df, y_pred=y_predict)))
    #bdj: may have to swap order of target names to reflect 1 as bystander not subject.
    print(classification_report(y_true=y_test_df, y_pred=y_predict, target_names=['subject', 'bystander']))
    xgboost.plot_importance(xgb)



if __name__ == '__main__':
    main()
