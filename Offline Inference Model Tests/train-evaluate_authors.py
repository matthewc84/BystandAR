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
    df = pd.read_excel('image-set_features.xlsx', header=0)
    lb = LabelEncoder()
    lb.fit(df['label'])
    label_col = lb.fit_transform(df['label'])
    df['label'] = label_col
    df.drop(labels=['center_deviation'], axis=1, inplace=True)
    print(df.describe())
    print(df['label'].value_counts())
    x_df = df.iloc[:, :-1]
    y_df = df.iloc[:, -1]
    x_train, x_test, y_train, y_test = train_test_split(x_df, y_df, test_size=0.1, shuffle=True)


    # train xgboost classifier
    parameters={'n_estimators':[100,200,300,400,500],'min_child_weight': [1, 5, 10],'subsample': [0.6, 0.8, 1.0],'colsample_bytree': [0.6, 0.8, 1.0],'max_depth': [6,8,10]}
    xgb = xgboost.XGBClassifier(max_depth=10, n_estimators=300, learning_rate=0.03, silent=True, subsample=0.7,
                                objective='binary:logistic') 
    xgb.fit(X=x_train, y=y_train)
    y_predict = xgb.predict(x_test)
    print('Accuracy score: ' + str(accuracy_score(y_true=y_test, y_pred=y_predict)))
    #bdj: may have to swap order of target names to reflect 1 as bystander not subject.
    print(classification_report(y_true=y_test, y_pred=y_predict, target_names=['subject', 'bystander']))
    xgboost.plot_importance(xgb)

    #TODO: could loop over all parameter combinations with cartesian products to see perf


if __name__ == '__main__':
    main()
